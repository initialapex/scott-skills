# Cypress / Infineon EZ-USB FX2LP (CY7C68013A, 8051)

Contents: what is different here · layering without paying for it · memory spaces and
pointers · function pointers and reentrancy · a compatibility header · interrupts and
SYNCDELAY · EP0 and vendor requests · endpoints, FIFOs, and GPIF · boot and download · host
tests · pitfalls

Scope: FX2LP firmware in C with SDCC (with or without `fx2lib`) or Keil C51. This chip is
an 8-bit 8051 with 16 KB of code/data RAM, a 256-byte internal stack area, and a USB 2.0
engine with endpoint FIFOs; it is **not** like the ARM parts in the rest of this skill,
and the object-oriented style has to be scaled down to it. Register names below are the
datasheet/TRM names; verify bit positions, addresses, and required delays in the EZ-USB
FX2LP Technical Reference Manual (TRM) and the datasheet of the exact part. Treat any
snippet as a pattern to check against the TRM, not as verified register code.

## What is different here

| Topic | ARM parts | FX2LP |
|-------|-----------|-------|
| Word size | 32-bit, `int` = 32 bits | 8-bit core, `int` = 16 bits; 16/32-bit operations are multi-instruction and not atomic |
| RAM | tens to hundreds of KB, uniform | 16 KB in `xdata`/`code` (unified when firmware is loaded into RAM) + 256 bytes of internal `idata` that hold the **hardware stack** |
| Pointers | one kind | memory-space-specific (`code`, `xdata`, `idata`, `data`) or slower generic pointers |
| Reentrancy | stack-based, free | function parameters and locals are normally **static and overlaid**, so a function called from main and an ISR (or recursively) corrupts itself unless made reentrant |
| Language | C99 | C89 with extensions (Keil), C89/partial C99 (SDCC) |
| Debug | SWD, breakpoints | usually printf over a serial pin, a vendor-request log, or a simulator |

Consequences for this skill: keep the **layer rule** (dependencies down; hardware access in
one place) because it is free; use **objects and ops tables sparingly**, where the benefit
(a table of handlers) exceeds the cost (indirect calls, 2- or 3-byte pointers, reentrancy).

## Layering without paying for it

- Layers are **files and naming discipline**: `drv_*.c` for external devices behind the
  FX2 (an I2C sensor), `svc_*.c` for logic, `fx2_*.c` for the register-level port (EP
  setup, GPIF, I2C controller, timers), `app.c` for behavior. The include rule is the
  same: no register names (`EP6CFG`, `IFCONFIG`) outside the `fx2_*` files.
- **Bind at compile or link time** instead of through ops tables ([oop-in-c.md](oop-in-c.md)):
  a driver calls `i2c_write()`, declared in `hal_i2c.h`, implemented by `fx2_i2c.c` on the
  chip and by `fake_i2c.c` in a host test. The call is direct and the linker's call
  analysis sees it.
- **Single-instance modules use file-scope state.** Overriding the general rule: with one
  I2C controller, one stream engine, and one vendor-command dispatcher, a `self` pointer
  costs code and a pointer parameter (up to 3 bytes) for no benefit. Keep the state in a
  `static` struct at the top of the `.c`, expose functions without `self`, and document
  "one instance". When several instances exist (a bank of identical endpoints), use an array of structs
  and pass an **index** (`uint8_t ep`), not a pointer.
- **Where tables are worth it:** the vendor-request dispatcher, descriptors (data, no code), a
  command table for a UART/I2C protocol, and register init lists (`{reg, value}` pairs
  applied in a loop, kept in code space).

## Memory spaces and pointers

| Space | SDCC | Keil C51 | Use for |
|-------|------|----------|---------|
| Direct internal RAM | `__data` | `data` | a few hot variables (counters, flags); 128 bytes shared with registers and stack |
| Indirect internal RAM | `__idata` | `idata` | stack lives here; leave room (see below) |
| Bit-addressable | `__bit`, `__sfr`, `__sbit` | `bit`, `sfr`, `sbit` | single-bit flags, ports |
| External (on-chip) RAM | `__xdata` | `xdata` | buffers, structs, most state (the 16 KB RAM) |
| Code | `__code` | `code` | `const` data: descriptors, tables, strings |

- **Declare the space of every pointer** (`const uint8_t __code *desc`, `uint8_t __xdata
  *buf`); a generic pointer carries a type byte (3 bytes in Keil) and every dereference
  dispatches on it. Memory-specific pointers are 1 or 2 bytes and fast.
- **`const` data in `code` space**: write `__code` (or `code`) explicitly; a plain `const`
  object may still be placed in RAM.
- **Internal stack budget**: all return addresses and interrupt contexts share the 256-byte
  `idata` area with register banks and `data` variables. Count call depth (2 bytes per call
  plus pushed registers) and ISR nesting; keep call chains shallow (about 8 to 10 levels at
  most), no recursion, and no helper layers that exist only to look tidy. Tools: the
  linker map (Keil `.m51`, SDCC `.mem`/`.map`) and the compiler's call-tree report.
- **16- and 32-bit values** in `xdata` shared with an ISR are not atomic: disable the
  interrupt around the access, or read twice until equal. This includes the byte-count
  register pairs (`EPxBCH:EPxBCL`).
- Use `unsigned char` (`uint8_t`) for loop counters and flags; the core is 8-bit and `int`
  arithmetic costs several instructions.

## Function pointers and reentrancy

- **SDCC** supports function pointers (2 bytes, code space; calls go through a helper), and
  `__reentrant` functions keep their locals on a software stack in `xdata`/`idata`
  (`--stack-auto`, `--xstack`), at a code-size and speed cost. **Keil C51** assigns parameters and
  locals to overlayable segments chosen by a call-tree analysis in the linker (BL51/LX51); a
  function reached only through a pointer may defeat that analysis (look for linker warnings
  about multiple calls to a segment or uncalled segments), and the `reentrant` keyword has
  the same costs.
- **Rule:** a function that can run in two contexts (main and ISR, or from two callers
  that may be active at once) must be `reentrant`, or the second context must not call it.
  Keep ISRs to flag-setting and register reads; they call nothing else, which also avoids
  saving a whole register bank.
- **Table of handlers is fine** if: the table is `const` in code space with the pointers
  declared as code-space function pointers; handlers are called from one context only (the
  main loop); and the build is checked for overlay warnings. A command dispatcher is the
  right size of OOP here.

```c
/* fx2_vendor.c (SDCC style; Keil: replace __code with code, __xdata with xdata) */
typedef struct vendor_cmd {
    uint8_t request;                                    /* bRequest */
    uint8_t dir_in;                                     /* 1 = device to host */
    uint8_t max_len;                                    /* largest accepted wLength */
    uint8_t (*handler)(uint16_t value, uint16_t index, uint8_t __xdata *buf, uint8_t *len);
} vendor_cmd_t;

static uint8_t cmd_get_version(uint16_t v, uint16_t i, uint8_t __xdata *buf, uint8_t *len);
static uint8_t cmd_set_mode(uint16_t v, uint16_t i, uint8_t __xdata *buf, uint8_t *len);

static const __code vendor_cmd_t s_cmds[] = {
    { 0xB0u, 1u, 4u, cmd_get_version },
    { 0xB1u, 0u, 2u, cmd_set_mode },
};
```

  Request numbers: do not use `0xA0` (the core's firmware-load request); avoid `0xA1` to
  `0xAF` as well, which Cypress loaders and tools use. Keep the request codes in a header
  shared with the host software.

## A compatibility header

One header hides the compiler differences so that the rest of the code and the host tests
compile unchanged:

```c
/* fx2_compat.h */
#ifndef FX2_COMPAT_H
#define FX2_COMPAT_H

#if defined(__SDCC)
  #define XDATA   __xdata
  #define CODE    __code
  #define IDATA   __idata
  #define BITVAR  __bit
  #define INTERRUPT(n) __interrupt(n)
  typedef unsigned char  uint8_t;   typedef signed char   int8_t;
  typedef unsigned short uint16_t;  typedef signed short  int16_t;
  typedef unsigned long  uint32_t;  typedef signed long   int32_t;
#elif defined(__C51__)
  #define XDATA   xdata
  #define CODE    code
  #define IDATA   idata
  #define BITVAR  bit
  #define INTERRUPT(n) interrupt n
  typedef unsigned char  uint8_t;   typedef signed char   int8_t;
  typedef unsigned int   uint16_t;  typedef signed int    int16_t;
  typedef unsigned long  uint32_t;  typedef signed long   int32_t;
#else                               /* host build for unit tests */
  #include <stdint.h>
  #define XDATA
  #define CODE
  #define IDATA
  #define BITVAR  unsigned char
  #define INTERRUPT(n)
#endif

#endif /* FX2_COMPAT_H */
```

(`stdint.h` is not available or not complete on these toolchains; define the types once.
The ISR keyword spelling differs, so the macro is used in the vector definitions only.)

## C dialect

- Treat the language as C89: declarations at the top of a block, no designated
  initializers, no `//` in Keil versions that do not accept it (use `/* */`), no `inline`,
  no `bool` (use `uint8_t` or the `bit` type for single flags), no variable-length arrays.
  SDCC accepts more (`--std-c99`, `--std-sdcc99`); stay with the common subset when the
  code must also build in Keil.
- Assign struct fields instead of using a config struct with designated initializers.
- Avoid `printf` family (huge code and RAM); write a small `put_hex`/`put_str` to a UART or
  to a vendor-request log ring buffer.
- Avoid 32-bit multiplication and division, floating point, and `%` in ISRs and
  time-critical code; they compile to library calls.

## Interrupts and SYNCDELAY

- The USB engine raises one 8051 interrupt (INT2 autovector) with sub-sources (`SUDAV`,
  `SOF`, `SUTOK`, `SUSP`, `URES`, `HSGRANT`, `EP0ACK`, plus endpoint and FIFO interrupts).
  Set up the autovector table (in `fx2lib`: `USE_USB_INTS()`, `ENABLE_SUDAV()`,
  `ENABLE_USBRESET()`, ...) and **clear both** the source flag in its USB interrupt
  register and the 8051 interrupt flag (the `EXIF` USB bit) in the ISR, or the interrupt
  fires again forever.
- **ISR = capture and signal**: set a `volatile` flag (`sudav_pending = 1;`) and return; the
  main loop (`TD_Poll()` in the Cypress framework, your `while (1)` in `fx2lib`) handles it.
  This is the interrupt-to-task rule of [interrupts-concurrency.md](interrupts-concurrency.md)
  with a one-task system.
- **`SYNCDELAY`**: many FX2 registers (FIFO configuration, `FIFORESET`, endpoint config,
  autopointer and similar) are synchronized to the internal clock, and consecutive writes
  or a write followed by a read need a short delay of a few instruction cycles, which depends
  on the CPU clock setting (`CPUCS.CLKSPD`). Missing delays cause intermittent
  misconfiguration that looks random. Use the `SYNCDELAY` macro everywhere the TRM requires
  it (the "Synchronization Delay" section lists which registers), and do not remove them to
  "clean up".
- **Register banks**: `using` (Keil) / `__using` (SDCC) gives an ISR its own bank so it need
  not save registers; one bank per priority level.

## EP0 control transfers and vendor requests

Pattern (names from the TRM; verify behavior against it):

1. The SETUP packet lands in `SETUPDAT[0..7]` and raises `SUDAV`; the ISR sets a flag and
   clears the interrupt source.
2. The main loop **copies `SETUPDAT` into a local struct** (`usb_setup_t`, see
   [usb-device.md](usb-device.md); build `value`, `index`, `length` from the little-endian bytes
   explicitly), because the next SETUP overwrites it.
3. Standard requests are handled by the framework (`fx2lib` / Cypress `fw.c`); class and
   vendor requests are dispatched through the command table above.
4. **Data stage**: for device-to-host, fill the `EP0BUF` and arm it by writing the byte count
   to `EP0BCH`/`EP0BCL`; for descriptors and long data use the setup-data pointer
   (`SUDPTRH`/`SUDPTRL`) with data in code or xdata. For host-to-device, wait for the OUT
   data (`EP0CS.OUTBSY`/the arm mechanism), then process it.
5. **Status stage**: set `EP0CS.HSNAK` to acknowledge the request (or `EP0CS.STALL` to reject
   it). Forgetting the handshake hangs the transfer, and the host reports a timeout.
- Unsupported request: stall, never ignore.
- Re-enumeration (`RENUM`, `USBCS.DISCON`/`CONT`) is how firmware changes the device
  identity after download.

## Endpoints, FIFOs, and GPIF

- **Endpoint configuration** (`EPxCFG`, `EPxFIFOCFG`, buffer counts, `FIFORESET` sequence) is
  done on `SET_CONFIGURATION`/`SET_INTERFACE` from the **same constants as the descriptors**
  (endpoint number, direction, type, max packet size, buffer count); a mismatch enumerates
  fine and then moves no data.
- **Autoin/autoout** (`EPxFIFOCFG.AUTOIN/AUTOOUT`, `EPxAUTOINLENH/L`) lets the FIFO engine
  move data between the interface and the USB without the 8051 touching each packet; this is
  how high-throughput streaming is done. The 8051 only configures and supervises it.
- **Interface modes** (`IFCONFIG`): internal clock (30/48 MHz) or external, **GPIF master**
  (the FX2 drives the external bus with a waveform), or **slave FIFO** (an external master
  such as an FPGA or ADC controller pushes data with `SLWR`, `SLRD`, `SLOE`, `PKTEND`,
  `FIFOADR[1:0]`, and reads the flags). The GPIF waveforms come from the Cypress **GPIF
  Designer** tool, which generates initialization code: treat the generated file like
  other generated code (regenerate from the project, do not hand-edit).
- **Flow control** is a design item, not a detail ([usb-device.md](usb-device.md)): decide
  what an external master does when the endpoint FIFOs are full (flags, back-pressure) and
  count overruns.
- **Short packets and `PKTEND`**: to end a transfer that is not a multiple of the packet
  size, commit a short packet (`INPKTEND`, or `PKTEND` from the slave interface).
- Registers specific to the FX2LP revision (such as `REVCTL`) need the settings the TRM and the
  framework give; copy them from the framework's init, do not omit them.

## Boot, download, and debugging

- With no EEPROM (or a blank one) the chip enumerates as a generic device and the host
  **downloads the firmware into RAM** through the control endpoint (the vendor request
  reserved for this, `0xA0`), holding the 8051 in reset via `CPUCS` while loading; tools:
  Cypress Control Center, `fx2load`, `cycfx2prog`, libusb scripts. With an I2C EEPROM the
  chip can boot from it (the first byte selects the boot mode: `0xC0` loads VID/PID/DID and
  keeps the generic device; `0xC2` loads the firmware image).
- Firmware size is bounded by the 16 KB shared with data, so measure it
  (`.map`/`.mem`/`.m51`) at every change; stay below the point where `xdata` buffers have
  no room.
- Debugging: a UART/GPIO trace, a vendor request that returns a **log ring buffer and
  counters** (overruns, stalls, last error), and `ucsim` (SDCC simulator) for logic.
  A logic analyzer on the GPIF/FIFO signals settles most timing questions.

## Host tests

The portable logic (the vendor dispatcher, parsers, state machines, descriptor builders)
compiles on a PC through `fx2_compat.h`'s host branch, with a fake `hal_i2c.c` for
drivers ([testing-host.md](testing-host.md)). Build the host tests in a 16-bit-`int` or
`-m32` configuration if the code depends on integer width, and keep register access out of
the portable files so they link without the chip's headers.

## Pitfalls

- A missing `SYNCDELAY`, or one removed during cleanup.
- Not clearing both the USB interrupt flag and the 8051 interrupt flag in the ISR.
- No handshake (`HSNAK`) on a control transfer, or a request left unanswered.
- Descriptors and endpoint configuration defined in two places and not matching.
- A non-reentrant function called from both main and an ISR (silent corruption).
- Call depth or ISR nesting overflowing the 256-byte `idata` stack; stack variables
  placed in `idata` by a memory model that was not chosen on purpose.
- Pointers without a memory space (generic pointers, slow and large), or a code-space
  table accessed through an `xdata` pointer (reads the wrong memory).
- Using `0xA0`-range vendor requests that collide with the firmware-load mechanism.
- Reading a 16-bit byte count while a packet arrives (torn read).
- Treating generated GPIF Designer code as editable source.
