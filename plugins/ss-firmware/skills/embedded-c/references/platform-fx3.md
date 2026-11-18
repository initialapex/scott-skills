# Cypress / Infineon EZ-USB FX3 (CYUSB301x, ARM926EJ-S, ThreadX)

Contents: the shape of an FX3 application · how the layers map · start-up · USB: setup
callback and events · endpoint and DMA channel setup · GPIF II · threads and events ·
memory and cache · errors · debug and boot · host tests · pitfalls

Scope: FX3 firmware in C with the EZ-USB FX3 SDK (the `CyU3P*` API, ThreadX RTOS, ARM GCC
toolchain). The firmware runs on an ARM926EJ-S with 512 KB of SRAM shared by code, data,
heap, and DMA buffers, a USB 3.0 device engine, the GPIF II programmable interface, and
I2C, I2S, SPI, UART, and GPIO blocks. Function and type names below are the SDK's
as commonly used; the SDK version and the FX3 Programmer's Manual and API guide are the
authority for signatures, callback contexts, and constants. Verify each call against the
SDK in use before relying on it.

## The shape of an FX3 application

- ThreadX is built in; the SDK starts it. Your `main()` initializes the device and hands
  control to the kernel, and the kernel calls `CyFxApplicationDefine()`, where the
  application creates its first thread.
- Everything else is **callbacks plus threads**: the USB driver calls your setup and event
  callbacks; DMA channels call completion callbacks; your **application thread** waits on
  event flags and does the slow work.
- Hardware blocks are configured through the **IO matrix** (which pins serve UART, I2C,
  SPI, I2S, and the 16- or 32-bit GPIF data bus): these share pins, so the choice is made
  once at start-up and is a hardware design decision.

This is a good fit for the objects-and-layers style: the core is ARM, indirect calls are
cheap, and the structure below applies without the 8051 compromises.

## How the layers map

| Layer | FX3 firmware |
|-------|--------------|
| app | `app_thread`: state (`DISCONNECTED`, `CONFIGURED`, `STREAMING`), reacts to events |
| function | `usb_vendor.c` (command table), `usb_desc.c` (const descriptors per speed) |
| stream | `stream.c`: owns the endpoint config, DMA channel, and GPIF start/stop as one object |
| driver | external devices on I2C/SPI (sensor, FPGA registers, EEPROM) against `hal_i2c`/`hal_spi` |
| hal | `hal_*` interfaces and a small `usb_port_ops_t` (below) for what the upper layers need from USB |
| port | `port_fx3_*.c`: the only files including `cyu3system.h`, `cyu3usb.h`, `cyu3dma.h`, `cyu3gpif.h`, `cyu3i2c.h`, ... |
| board | `board_fx3.c`: IO matrix, clocks, PIB, which pins are what |

```c
/* The seam between function/stream layers and the SDK: a fake for host tests, the real thing on target. */
typedef struct usb_port usb_port_t;
typedef struct usb_port_ops {
    fw_status_t (*send_ep0)(usb_port_t *self, const uint8_t *data, uint16_t len);
    fw_status_t (*get_ep0)(usb_port_t *self, uint8_t *buf, uint16_t len, uint16_t *out_len);
    fw_status_t (*ack_setup)(usb_port_t *self);
    fw_status_t (*stall_ep0)(usb_port_t *self);
    fw_status_t (*config_endpoints)(usb_port_t *self, usb_speed_t speed);
    fw_status_t (*flush_endpoint)(usb_port_t *self, uint8_t ep_addr);
} usb_port_ops_t;
struct usb_port { const usb_port_ops_t *ops; };
```

## Start-up

Typical order (see the SDK's sample projects for the exact code of your version):

```c
int main(void)
{
    CyU3PIoMatrixConfig_t io;
    CyU3PReturnStatus_t st;

    st = CyU3PDeviceInit(NULL);                     /* clocks default; NULL = default config */
    if (st != CY_U3P_SUCCESS) { fatal(st); }
    st = CyU3PDeviceCacheControl(CyTrue, CyTrue, CyTrue);   /* I-cache, D-cache, DMA-aware handling */
    if (st != CY_U3P_SUCCESS) { fatal(st); }

    CyU3PMemSet((uint8_t *)&io, 0, sizeof(io));
    io.isDQ32Bit = CyFalse;                         /* 16-bit GPIF data bus: frees pins */
    io.useUart = CyTrue;                            /* debug UART */
    io.lppMode = CY_U3P_IO_MATRIX_LPP_UART_ONLY;    /* names vary: use the sample for your SDK */
    st = CyU3PDeviceConfigureIOMatrix(&io);
    if (st != CY_U3P_SUCCESS) { fatal(st); }

    CyU3PKernelEntry();                             /* never returns */
    return 0;
}

void CyFxApplicationDefine(void)                    /* called by the kernel: create the app thread */
{
    void *stack = CyU3PMemAlloc(APP_STACK_SIZE);    /* once, at start-up */
    if (stack == NULL ||
        CyU3PThreadCreate(&s_app_thread, "21:app", app_thread_entry, 0, stack, APP_STACK_SIZE,
                          APP_THREAD_PRIO, APP_THREAD_PRIO, CYU3P_NO_TIME_SLICE,
                          CYU3P_AUTO_START) != CY_U3P_SUCCESS) {
        fatal(0);
    }
}
```

The SDK samples stop in an error handler (`CyFxAppErrorHandler`) on any failure. In a
product, **map `CyU3PReturnStatus_t` to `fw_status_t` in the port**, log the code once, and
recover or reset deliberately ([robustness.md](robustness.md)). The thread name string has
the form `"<id>:<name>"`.

## USB: setup callback and events

```c
CyU3PUsbRegisterSetupCallback(setup_cb, CyTrue);   /* second argument: fast enumeration */
CyU3PUsbRegisterEventCallback(event_cb);
CyU3PUsbSetDesc(CY_U3P_USB_SET_SS_DEVICE_DESCR, 0, (uint8_t *)s_ss_device_desc);   /* per speed, per type */
/* ...high-speed and full-speed device descriptors, configuration descriptors, BOS, strings... */
CyU3PUsbStart();
CyU3PConnectState(CyTrue, CyTrue);                  /* connect, SuperSpeed enabled */
```

- **Setup callback** `CyBool_t setup_cb(uint32_t setupdat0, uint32_t setupdat1)`: return
  `CyTrue` when the request was handled (including stalling it) and `CyFalse` to let the SDK
  process the standard request. Decode the two words once into a plain struct
  (`usb_setup_t` in [usb-device.md](usb-device.md)):

```c
static void decode_setup(uint32_t d0, uint32_t d1, usb_setup_t *s)
{
    s->request_type = (uint8_t)(d0 & 0xFFu);
    s->request      = (uint8_t)((d0 >> 8) & 0xFFu);
    s->value        = (uint16_t)(d0 >> 16);
    s->index        = (uint16_t)(d1 & 0xFFFFu);
    s->length       = (uint16_t)(d1 >> 16);
}
```

  Then route vendor requests to the command table ([usb-device.md](usb-device.md)); the
  handler answers through the port: `CyU3PUsbSendEP0Data(len, buf)` for a device-to-host data
  stage, `CyU3PUsbGetEP0Data(len, buf, &read)` for host-to-device, `CyU3PUsbAckSetup()` for
  no data stage, `CyU3PUsbStall(0, CyTrue, CyFalse)` to reject.
- **EP0 buffers** are global and cache-line aligned
  (`static uint8_t s_ep0_buf[32] __attribute__((aligned(32)));`), as in the SDK samples.
- **Event callback** `void event_cb(CyU3PUsbEventType_t ev, uint16_t data)` reports
  connect, `SETCONF` (host selected a configuration), `SETINTF`, reset, suspend, resume,
  disconnect. **Do not start streaming here.** Post an event flag; the app thread does
  the work, and the same thread tears everything down on reset or disconnect.
- **Callback context**: the samples call the EP0 functions inside the setup callback and
  non-blocking DMA calls inside DMA callbacks; for anything else, assume the callback may
  be running in a driver context that must not wait, copy what you need, signal the app
  thread, and return. Confirm the context of each callback in the Programmer's Manual.

## Endpoint and DMA channel setup

On `SETCONF` (and `SETINTF`), in the app thread:

1. Read the speed (`CyU3PUsbGetSpeed()`) and choose the packet size (bulk: 64 full speed,
   512 high speed, 1024 SuperSpeed) and burst length.
2. Configure endpoints with `CyU3PSetEpConfig(ep_addr, &epCfg)` (`epCfg.enable`, `epType`,
   `burstLen`, `streams`, `pcktSize`), from the **same constants as the descriptors**.
3. Create a DMA channel between the USB socket and the other side (another socket, or the CPU)
   with `CyU3PDmaChannelCreate(&ch, type, &cfg)`; set `cfg.size` (buffer size, a multiple of
   the packet size and cache line), `cfg.count` (number of buffers), `cfg.prodSckId`,
   `cfg.consSckId`, `cfg.dmaMode`, and callback/notification options. Socket naming is
   from the DMA's view: a USB OUT endpoint (host to device) is a **producer** socket
   (`CY_U3P_UIB_SOCKET_PROD_n`), a USB IN endpoint a **consumer** socket
   (`CY_U3P_UIB_SOCKET_CONS_n`); GPIF-side sockets are `CY_U3P_PIB_SOCKET_n`.
4. Start it: `CyU3PDmaChannelSetXfer(&ch, 0)` (0 = no size limit), and start the GPIF state
   machine if the other end is GPIF.

Channel types: **AUTO** (the hardware moves buffers; best throughput, no CPU per buffer),
**AUTO_SIGNAL** (as AUTO with notifications), and **MANUAL** (the CPU gets, edits, and commits
each buffer through `CyU3PDmaChannelGetBuffer` / `CyU3PDmaChannelCommitBuffer`, used for
protocol processing). Use AUTO whenever the CPU does not need the data: streaming speed is
decided by this choice.

A **stream object** wraps all of this, with symmetric operations (the same shape as any
object in [oop-in-c.md](oop-in-c.md)):

```c
typedef struct stream {
    CyU3PDmaChannel channel;      /* SDK object: only the port or stream file touches it */
    stream_state_t state;
    uint32_t overruns;            /* counted, readable through a vendor request */
} stream_t;

fw_status_t stream_start(stream_t *self, usb_speed_t speed);   /* configure endpoints, create channel, start */
fw_status_t stream_stop(stream_t *self);                       /* idempotent: safe in any state */
```

`stream_stop` aborts and destroys the channel (`CyU3PDmaChannelAbort`,
`CyU3PDmaChannelDestroy`), flushes the endpoints (`CyU3PUsbFlushEp`), and disables GPIF, and
is called on reset, disconnect, `SETCONF(0)`, and before every `stream_start`. A stream that
only knows how to start leaks channels on re-enumeration.

## GPIF II

- The interface is designed in the **GPIF II Designer** tool (states, transitions, data
  and address bus, flags) and generates a header (such as `cyfxgpif2config.h`) with the
  register values and waveform tables. Treat it as generated code: keep the Designer
  project in version control, regenerate, do not hand-edit.
- Initialization order: `CyU3PPibInit(CyTrue, &pibClock)` with the PIB clock config, then
  `CyU3PGpifLoad(&CyFxGpifConfig)`, then configure the socket
  (`CyU3PGpifSocketConfigure(...)`, including the watermark), then
  `CyU3PGpifSMStart(START_STATE, ALPHA_START_STATE)` when the stream starts; the DMA
  channel connects a PIB socket to a UIB socket.
- Slave FIFO (an external master drives the interface) and GPIF master (the FX3 drives)
  are different Designer projects and different timing: put the choice in the board
  file, and count FIFO overruns and underruns (the external master's behavior is the
  usual cause of mysterious data loss).

## Threads and events

- **Event flags** connect callbacks to the app thread: `CyU3PEventCreate(&evt)`;
  callbacks call `CyU3PEventSet(&evt, EVT_X, CYU3P_EVENT_OR)`; the thread blocks on
  `CyU3PEventGet(&evt, EVT_ALL, CYU3P_EVENT_OR_CLEAR, &flags, CYU3P_WAIT_FOREVER)` and
  dispatches on the bits. This is the ISR-to-task hand-off of
  [interrupts-concurrency.md](interrupts-concurrency.md) with the SDK's primitives.
  Queues (`CyU3PQueue*`), mutexes (`CyU3PMutex*`), and timers (`CyU3PTimer*`) wrap ThreadX;
  the native `tx_*` API is also available.
- **One thread owns each resource**: the app thread owns the stream and GPIF; a second
  thread for slow peripherals (I2C EEPROM, SPI flash) keeps the USB path responsive.
  Give threads explicit priorities and sizes (a debug-print thread needs more stack than
  a callback-only one), and measure stack use.
- **`CyU3PThreadSleep(ms)`** is the delay; avoid busy-waiting on registers.
- Wait constants: `CYU3P_NO_WAIT`, `CYU3P_WAIT_FOREVER`; give every wait a bound unless it is
  the thread's main wait.

## Memory and cache

- 512 KB total: firmware image, thread stacks and objects (`CyU3PMemAlloc` heap),
  **DMA buffers** (a separate buffer area managed with the SDK's DMA buffer allocator; the
  heap and buffer area sizes are set in the SDK's `cyfxtx.c`), and the GPIF/USB structures.
  Check the budget at design time: buffer count x buffer size per channel is the biggest
  consumer.
- Allocate at start-up (stacks, channels, buffers) and do not allocate in steady state;
  the SDK heaps are not designed for fragmenting use.
- **Cache**: with the D-cache on, DMA buffers must be **32-byte aligned and a multiple of
  32 bytes**, and the SDK's DMA-aware handling (the third argument of
  `CyU3PDeviceCacheControl`) cleans/invalidates for DMA channels it manages; for buffers you
  hand to the CPU or share with hardware outside those channels, use the SDK's cache
  maintenance functions explicitly (see the cache section of the API guide for the names in
  your SDK version). EP0 buffers and buffers passed to the peripheral drivers follow the same
  alignment rule.

## Errors

- Every `CyU3P*` call returns `CyU3PReturnStatus_t` (`CY_U3P_SUCCESS` = 0). Map in the port
  (`CY_U3P_ERROR_TIMEOUT` -> `FW_ERR_TIMEOUT`, `CY_U3P_ERROR_BAD_ARGUMENT` ->
  `FW_ERR_INVALID_ARG`, `CY_U3P_ERROR_MEMORY_ERROR` -> `FW_ERR_NO_MEM`, others -> `FW_ERR_IO`;
  confirm the constants in `cyu3error.h`).
- A DMA error callback (`CY_U3P_DMA_CB_ERROR`) or a USB reset event is a **recovery
  trigger**: post an event; the app thread calls `stream_stop`, counts the error, and
  restarts the stream when the host re-configures. Do not retry inside the callback.

## Debug and boot

- **Debug prints**: `CyU3PDebugInit()` (UART or USB debug channel) +
  `CyU3PDebugPreamble(CyFalse)` + `CyU3PDebugPrint(level, "fmt", ...)`; they use a thread/DMA and
  consume stack and time, so rate-limit them and keep them out of callbacks. A vendor
  request that returns counters and a small log buffer is the reliable field tool.
- **Boot**: USB boot (the host downloads the image into RAM: development and some products), or
  from I2C EEPROM or SPI flash with an image made by the SDK tools from the ELF; confirm the
  boot mode pins (PMODE) in the board design. Programming the boot memory is a device
  change: show the tool command and let the developer run it.
- JTAG debugging through the SDK's Eclipse environment and a supported probe.

## Host tests

The function and stream layers depend on `usb_port_ops_t` and a stream/DMA wrapper
interface, so a fake port lets host tests cover the vendor dispatcher (feed parsed SETUP
packets, check data stage, stall, and ACK), descriptor builders (lengths, endpoint
addresses per speed), and the stream state machine (start, stop, restart after reset, error
counting). See [testing-host.md](testing-host.md); no `cyu3*.h` in those files.

## Pitfalls

- DMA buffers not aligned to or sized in multiples of 32 bytes (corrupt data under D-cache).
- Starting or stopping streams inside the USB event callback instead of the app thread.
- Endpoint configuration that does not match the descriptor for the speed in use (packet
  size, burst).
- Forgetting to answer a control transfer (`CyU3PUsbAckSetup`, send data, or stall): the host
  hangs for seconds, then resets.
- Missing teardown on reset and disconnect, so the second connection fails or leaks channels.
- Thread stacks too small for debug printing; stacks and channels allocated repeatedly.
- IO matrix configured for peripherals the board does not wire, or two features that share
  pins.
- Ignoring `CyU3PReturnStatus_t`; samples' infinite-loop error handlers left in product code.
- Using a GPIF II Designer project and an endpoint/DMA configuration that disagree on bus
  width, buffer size, or socket.
