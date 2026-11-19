# Errors, memory, types, and faults

Contents: status codes and error policy · asserts · watchdog and fault handling · static
memory and pools · integer and type pitfalls · wire and register formats · compile-time
configuration · persistent data · warnings and analysis

## Status codes and error policy

- One status type across layer boundaries (`fw_status_t`, 0 = OK; see
  `templates/common/fw_common.h`). Ports translate vendor codes
  ([hal-osal-ports.md](hal-osal-ports.md)). Do not return `-1`, `0/1` booleans, or a
  mix of `bool` and `int` for failure in the same module.
- **Output through pointers, status as the return value** (`fw_status_t foo_read(foo_t *self,
  int32_t *out)`), and leave `*out` untouched on failure so callers cannot use garbage that
  looks valid. A function that cannot fail returns the value directly.
- **Decide the policy per operation and write it in the header:**

| Class | Examples | Policy |
|-------|----------|--------|
| Transient | `FW_ERR_BUSY`, `FW_ERR_TIMEOUT` on a shared bus | retry a bounded number of times with a back-off, then report |
| Device or data error | NACK, CRC mismatch, `FW_ERR_IO` | count it, report upward, let the service decide (fault after N in a row) |
| Caller bug | `FW_ERR_INVALID_ARG`, `FW_ERR_INVALID_STATE` | return it in release; catch it with an assert in debug |
| Resource exhaustion | `FW_ERR_NO_MEM`, `FW_ERR_OVERFLOW` | drop with a counter, never block an ISR; sizes chosen at design time |
| Unrecoverable | corrupted state, failed init of a required part | go to a safe state, log the reason, reset |

- **Check every return value.** If a result is deliberately ignored, cast it `(void)` and
  say why in a comment (`(void)led_set(...); /* cosmetic, no recovery possible */`).
  Compilers can enforce it (`__attribute__((warn_unused_result))`, wrapped in a macro).
- **Propagate with early returns**; use one `goto fail` cleanup path only where several
  resources were acquired in order (release in reverse order). Do not nest five levels of
  `if (st == FW_OK)`.
- **Log an error once, where it is decided,** not at every layer it passes. Include the
  vendor code at the port, the operation and the device at the driver.
- **No error hidden in a value**: `0` meaning "no data" and "sensor failed" at once is a
  bug waiting. Use a status or a validity flag.
- **Timeouts are errors too**: a bus transfer that times out leaves the bus in an unknown
  state; the port recovers it (toggle clock lines, reset the peripheral) before the next call,
  or reports `FW_ERR_IO` until it has.

## Asserts

Two different tools; do not mix them:

- **Return a status** for anything that can happen with valid code (bad input from a
  packet, a missing device, a full queue).
- **`FW_ASSERT(cond)`** for conditions that mean the program itself is wrong (an impossible
  enum value, a null `self` passed to an internal function, a violated invariant).

Define the assert policy once: in debug, stop in the debugger; in release, record file and
line in a `noinit` RAM area (survives a reset), then reset or enter the safe state. Never
use `assert()` from the C library on a target (it pulls in `printf` and `abort`), and
never put a side effect inside the assert expression (release builds may compile it out).

## Watchdog and fault handling

- **A watchdog is only useful if feeding it proves the system works.** Do not feed it from
  a timer ISR (it will happily feed a dead system). A supervisor task feeds it only when
  every monitored task has checked in since the last feed (a bitmask each task sets).
- **Capture the cause of a reset** at boot (reset-reason register: power-on, brown-out,
  watchdog, software, pin) and keep it in the log or a counter. "It rebooted" without a
  reason cannot be debugged.
- **HardFault / panic handler**: save the stacked PC, LR, and fault status registers
  (Cortex-M: CFSR, HFSR, MMFAR, BFAR) to `noinit` RAM or flash, then reset. ESP-IDF
  (core dumps), Zephyr (`CONFIG_RESET_ON_FATAL_ERROR`, fatal error handler), and the other
  SDKs have hooks; use them rather than an endless `while (1)`.
- **Safe outputs at reset**: define the pin state of each actuator before the firmware
  runs (external pull resistors, early GPIO init) and when it stops (fault handler sets
  heaters, motors, and radios off first).
- **Brown-out and power loss**: assume the supply can drop at any instruction; see the
  persistent-data rules below.

## Static memory and pools

- **No `malloc`/`free` after start-up.** Heap fragmentation and exhaustion appear after
  days in the field. When an SDK allocates internally (ESP-IDF Wi-Fi, Zephyr Bluetooth
  buffers), reserve the budget and monitor the free-heap low-water mark.
- **Object storage is static or caller-provided** (`static foo_t s_foo;` in the board
  file; `foo_init(&s_foo, &cfg)`).
- **Variable-lifetime data uses a fixed-block pool** (messages, packets, events): a static
  array of blocks, a free list, `acquire` returns `NULL` or `FW_ERR_NO_MEM` when empty, `release`
  returns the block; for ISR use, the free list is protected by a critical section or is a
  lock-free stack/queue. Choose the block size and count from the worst case and add a
  counter of the lowest free count.
- **Where things live.** `static const` data goes to flash; `const` on the pointer
  *and* the pointee for tables of strings. On Harvard cores (8051, AVR) a plain `const`
  may still be copied to RAM; use the compiler's code-space keyword (`__code`, `code`,
  `PROGMEM`). Initialized non-const globals cost flash *and* RAM.
- **Know the budget.** Read the linker map after each feature: `arm-none-eabi-size`,
  `idf.py size`, `west build -t rom_report ram_report`, Keil `.m51`, SDCC `.mem`. Track
  `.bss`, `.data`, flash, and stack high-water; fail CI when a budget is exceeded.
- **No recursion, no variable-length arrays, no `alloca`**: stack use must be computable.
  Compiler support: `-fstack-usage`, `-Wstack-usage=`, `-Wvla`.
- **Large locals are static members of the object**, not stack variables (see stacks in
  [interrupts-concurrency.md](interrupts-concurrency.md)).

## Integer and type pitfalls

- Use `<stdint.h>` types (`uint8_t`, `int32_t`), `size_t` for sizes and counts, `bool` for
  flags. Plain `int` is 16 bits on 8051 and AVR and 32 on ARM; code that must work on
  both must not depend on it.
- **Integer promotion**: `uint8_t << 8` happens in `int`; on a 16-bit `int` that overflows.
  Cast first: `((uint16_t)b[0] << 8) | b[1]`, and `(uint32_t)x * y` before multiplying
  16-bit values that can exceed 65535.
- **Signed overflow is undefined behavior**; shifting into the sign bit too. Use unsigned
  types for bit patterns and counters, signed only for values that are signed.
- **Sign extension by hand** when a field is narrower than its container (12-bit
  two's complement: `if (raw & 0x800) raw -= 0x1000;`, as in the TMP102 driver).
- **Division and modulo** by a power of two should be a shift or mask on cores without a
  hardware divider (Cortex-M0, 8051); avoid 32/64-bit division in ISRs.
- **Floating point**: on a core without an FPU it is emulated and slow; `float` literals
  need the `f` suffix or the expression is promoted to `double`. Prefer fixed-point or
  integer units (milli-degrees, millivolts) at interfaces, as `sensor_t` does.
- **Enums**: their size is compiler-chosen. Use an enum for logic, and a fixed-width
  integer for what is stored or sent. Validate values that arrive from outside before
  using them as an enum or an array index.
- **Comparisons across signedness** (`int` vs `size_t`) hide negative-value bugs; enable
  `-Wsign-compare` and `-Wconversion`.

## Wire and register formats

- **Never overlay a struct on a byte buffer** to parse a packet, and never use a C
  bit-field for a hardware register or wire format: member layout, padding, and bit
  order are implementation-defined and differ between compilers; unaligned access faults on
  some cores and is slow on others; strict aliasing makes it undefined.
- **Serialize and deserialize field by field** with explicit offsets and byte order
  helpers (`fw_get_be16`, `fw_put_le32`, ...). Define offsets as named constants or one
  `parse`/`build` function per message, tested on the PC with captured frames.

```c
#define FRAME_OFF_LEN   0u
#define FRAME_OFF_CMD   2u
#define FRAME_HDR_SIZE  3u

fw_status_t frame_parse(const uint8_t *buf, size_t len, frame_t *out)
{
    if (len < FRAME_HDR_SIZE) { return FW_ERR_INVALID_ARG; }
    out->length = fw_get_be16(&buf[FRAME_OFF_LEN]);
    out->command = buf[FRAME_OFF_CMD];
    if ((size_t)out->length + FRAME_HDR_SIZE > len) { return FW_ERR_INVALID_ARG; }
    out->payload = &buf[FRAME_HDR_SIZE];
    return FW_OK;
}
```

- **Check the length before reading,** and check that length fields do not overflow when
  added. Every parser of external data is an attack surface and a crash source.
- **Register access** through the SDK's headers (`GPIOA->ODR`, `LPUART1->STAT`) inside
  ports only; where raw addresses are needed, one `volatile` accessor macro per register
  with masks and shifts named (`REG_CTRL_ENABLE_Msk`). Read-modify-write on a register
  shared with an ISR is a critical section or a bit-set/clear register.
- **Packed structures** are acceptable only for data that is never accessed by pointer on
  a core that faults on unaligned access, and are still checked with
  `FW_STATIC_ASSERT(sizeof(x) == N, x_size)`; serializing by hand is simpler and safer.
- **Endianness**: Cortex-M, ESP32 (Xtensa/RISC-V), nRF, and i.MX RT are little-endian; the
  8051 FX2 is big-endian for multi-byte values in Keil C51 but SDCC is little-endian;
  USB descriptors are little-endian on the wire; network protocols are big-endian. Use
  helpers; never rely on the host's order.

## Compile-time configuration

- One configuration header or the SDK's own system (`sdkconfig`/Kconfig for ESP-IDF and
  Zephyr, `FreeRTOSConfig.h`, `fsl_*` macros) holds feature switches and sizes; no magic
  numbers in modules (`UART_RX_BUFFER_SIZE`, `THERMAL_POLL_MS`).
- **`#if` only at the boundary.** Conditional compilation inside driver logic makes every
  variant untestable. Select between implementations by which source file is built, or by
  an ops table chosen in board code.
- Use `FW_STATIC_ASSERT` to check configuration (`BUFFER_SIZE` is a power of two, a queue
  depth fits in `uint8_t`).

## Persistent data

- Store configuration and calibration with a **version, a length, and a CRC**; reject on
  mismatch and fall back to defaults (and say so in the log).
- **Power-fail-safe update**: write the new copy to a spare location, verify its CRC, then
  commit with a single small write (a sequence counter or a valid marker); keep two
  copies and take the newest valid one. Never erase the only good copy first.
- **Wear**: write only when the value changed, batch writes, rotate through sectors for
  frequently updated data (or use the SDK's storage layer: ESP-IDF NVS, Zephyr NVS/ZMS or
  settings, NXP and STM32 EEPROM emulation).
- Remember flash writes stall the CPU or the instruction fetch on some parts (ESP32 cache
  off, single-bank STM32): keep time-critical interrupts in RAM or defer the write.

## Warnings and analysis

- Build with `-Wall -Wextra -Wshadow -Wconversion -Wundef -Wstrict-prototypes
  -Wmissing-prototypes` and treat warnings as errors in CI; fix, do not silence.
  Vendor code is built with its own flags (do not edit it); keep your code in separate
  targets so your flags apply to it.
- Run `cppcheck` or `clang-tidy` in CI; run the host tests under AddressSanitizer and
  UBSan (`run-tests.sh`) because they find the buffer overruns and bad shifts a target
  cannot show cheaply.
- If MISRA-C is required, run the project's checker, record deviations with a reason, and
  keep the deviations next to the code (see [coding-style.md](coding-style.md)).
