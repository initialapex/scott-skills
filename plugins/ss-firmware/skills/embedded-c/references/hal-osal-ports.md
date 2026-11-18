# HAL interfaces, ports, and OSAL

Contents: what a HAL interface is for · designing one · sketches for GPIO, I2C, SPI, UART,
clock, flash, ADC · bus and device split · writing a port · status mapping · OSAL ·
time and timeouts · when to skip the abstraction

## What a HAL interface is for

The vendor HAL (STM32 HAL, ESP-IDF drivers, Zephyr drivers, `fsl_*`) is **not** this layer.
It is what a port calls. A **hal interface** is a header you own that states what the
upper layers need from a peripheral class, in your project's units and error type, so
that:

- drivers and services compile and test on a PC;
- changing MCU replaces ports, not drivers;
- vendor quirks (shifted I2C addresses, handle types, callback signatures, `esp_err_t`
  versus `HAL_StatusTypeDef`) stay in one file.

If the product is committed to one MCU family and the code will never run on a PC, a
thin interface is still worth it for the part of the code that has logic (drivers,
services), and not worth it for glue that only wires peripherals together.

## Designing an interface

1. **Start from the consumer's needs.** What does a sensor driver do on I2C? Write one
   register, read one or more registers, nothing else. Do not mirror the vendor's API.
2. **Fix the units and conventions in the header**: 7-bit addresses, timeouts in
   milliseconds (`0` = do not wait, a named constant for "forever"), frequencies in Hz,
   lengths in bytes, levels as `bool`, time as `uint32_t` milliseconds.
3. **Blocking with a timeout is the default**; offer an asynchronous variant (start +
   completion callback with `ctx`) only for the consumers that cannot block (UART RX, DMA
   streaming, ISR-driven flows). Say in the header which context may call each method.
4. **One bus object serializes its own transfers.** The port takes a mutex (or the
   RTOS's bus lock) around a whole transaction, so two drivers sharing a bus do not
   interleave. Callers never lock a bus.
5. **Map errors to `fw_status_t` in the port**, with a documented meaning for each code
   (NACK -> `FW_ERR_IO`, deadline missed -> `FW_ERR_TIMEOUT`, bus busy -> `FW_ERR_BUSY`).
6. **Buffer ownership is explicit.** For asynchronous calls, state that the buffer must
   stay valid until the completion callback, and (with DMA) that it must be in DMA-able
   memory and cache-line aligned (see [interrupts-concurrency.md](interrupts-concurrency.md)).
7. **Keep it small.** An interface is the intersection of what consumers need and what all
   targets can do. Optional capabilities are separate interfaces or `NULL` methods.

## Sketches

```c
/* hal_gpio.h */
typedef void (*hal_gpio_cb_t)(void *ctx);            /* edge callback, ISR or task: documented */
struct hal_gpio_ops {
    fw_status_t (*write)(hal_gpio_t *self, bool level);
    fw_status_t (*read)(hal_gpio_t *self, bool *out_level);
    fw_status_t (*set_edge_callback)(hal_gpio_t *self, hal_gpio_edge_t edge, hal_gpio_cb_t cb, void *ctx);
};

/* hal_spi.h - a device = bus + chip select + mode + clock, so CS handling is the port's job */
struct hal_spi_device_ops {
    fw_status_t (*transfer)(hal_spi_device_t *self, const uint8_t *tx, uint8_t *rx, size_t len,
                            uint32_t timeout_ms);   /* tx or rx may be NULL; CS asserted for the whole call */
    fw_status_t (*transfer_chained)(hal_spi_device_t *self, const hal_spi_segment_t *segs,
                                    size_t count, uint32_t timeout_ms); /* optional: command + data, one CS */
};

/* hal_uart.h - a byte stream; the port owns the RX ring buffer so no byte is lost between reads */
struct hal_uart_ops {
    fw_status_t (*write)(hal_uart_t *self, const uint8_t *data, size_t len, uint32_t timeout_ms);
    fw_status_t (*read)(hal_uart_t *self, uint8_t *buf, size_t max_len, size_t *out_len, uint32_t timeout_ms);
    fw_status_t (*set_rx_callback)(hal_uart_t *self, hal_uart_rx_cb_t cb, void *ctx); /* optional, ISR-safe contract */
};

/* hal_clock.h - monotonic milliseconds, wraps at 2^32 */
typedef struct hal_clock hal_clock_t;
struct hal_clock_ops { uint32_t (*now_ms)(hal_clock_t *self); };

/* hal_flash.h - geometry first: erase and write granularity decide how a service uses it */
struct hal_flash_ops {
    fw_status_t (*read)(hal_flash_t *self, uint32_t addr, uint8_t *buf, size_t len);
    fw_status_t (*write)(hal_flash_t *self, uint32_t addr, const uint8_t *data, size_t len);
    fw_status_t (*erase)(hal_flash_t *self, uint32_t addr, size_t len);   /* multiple of erase_size */
};
struct hal_flash {
    const hal_flash_ops_t *ops;
    size_t size;         /* geometry is per instance, see the note below */
    size_t erase_size;
    size_t write_size;
};

/* hal_adc.h */
struct hal_adc_ops { fw_status_t (*read_mv)(hal_adc_t *self, uint32_t *out_mv); };
```

Geometry (`erase_size`, `write_size`, `size`) is a property of the instance, not of the
implementation, so it goes in the object (as above) or in getter methods, not in the
shared const table. A flash service reads it and adapts; it does not hard-code a sector
size.

Two interface decisions that save later rewrites:

- **Pass the address on every I2C call, or one object per device?** Per call (as in the
  template) gives one bus object and lets a port with per-device handles (ESP-IDF) cache
  them; one object per device (`hal_i2c_device_t` = bus + address) hides the address from
  the driver. Either is fine; choose one and keep it across ports.
- **Chip select is not the driver's business.** SPI drivers talk to a `hal_spi_device_t`
  and never toggle a GPIO to select the chip; otherwise two tasks interleave transfers.

## Writing a port

A port file is a thin adapter: vendor types in, interface out. It holds the vendor handle,
the lock, and the error mapping, and nothing else.

```c
/* port_vendor_i2c.c - skeleton; replace the vendor calls with the SDK's */
typedef struct port_i2c {
    hal_i2c_t base;                 /* first member */
    vendor_i2c_handle_t *handle;    /* owned by board code */
    osal_mutex_t lock;
} port_i2c_t;

static fw_status_t map_status(vendor_status_t s)
{
    switch (s) {
    case VENDOR_OK:      return FW_OK;
    case VENDOR_TIMEOUT: return FW_ERR_TIMEOUT;
    case VENDOR_BUSY:    return FW_ERR_BUSY;
    default:             return FW_ERR_IO;     /* NACK, arbitration lost, bus error */
    }
}

static fw_status_t port_i2c_write_read(hal_i2c_t *base, uint8_t addr7, const uint8_t *tx,
                                       size_t tx_len, uint8_t *rx, size_t rx_len, uint32_t timeout_ms)
{
    port_i2c_t *self = FW_CONTAINER_OF(base, port_i2c_t, base);
    fw_status_t st;

    if (osal_mutex_lock(&self->lock, timeout_ms) != FW_OK) { return FW_ERR_BUSY; }
    st = map_status(vendor_i2c_transfer(self->handle, addr7, tx, tx_len, rx, rx_len, timeout_ms));
    osal_mutex_unlock(&self->lock);
    return st;
}

static const hal_i2c_ops_t s_ops = { port_i2c_write, port_i2c_write_read };

fw_status_t port_i2c_init(port_i2c_t *self, const port_i2c_config_t *cfg)
{
    /* validate, store the handle, create the mutex, set self->base.ops = &s_ops */
}
```

Port rules:

- **No device knowledge and no product logic.** "Read register 0x00 of the TMP102" is a
  driver; "write these bytes to an address" is a port.
- **Start from what the SDK already does.** If the SDK already serializes, times out, and
  reports errors (Zephyr `i2c_write_read`, ESP-IDF `i2c_master_transmit_receive`), the port
  is a handful of lines. Do not rebuild the SDK's work.
- **Run the same tests against every port** where possible: a hardware-in-the-loop test
  with a known device (EEPROM, a loopback UART, two GPIOs wired together) exercises the
  contract the host tests assume.
- **Vendor callbacks end here.** The port registers the vendor callback, finds the
  `port_*` object, and calls the interface's `cb(ctx, ...)`.

## Status mapping

| Vendor | Success | Common failures -> `fw_status_t` |
|--------|---------|----------------------------------|
| ESP-IDF `esp_err_t` | `ESP_OK` | `ESP_ERR_TIMEOUT` -> `FW_ERR_TIMEOUT`; `ESP_ERR_INVALID_ARG` -> `FW_ERR_INVALID_ARG`; `ESP_ERR_INVALID_STATE` -> `FW_ERR_INVALID_STATE`; `ESP_ERR_NO_MEM` -> `FW_ERR_NO_MEM`; others -> `FW_ERR_IO` |
| STM32 `HAL_StatusTypeDef` | `HAL_OK` | `HAL_TIMEOUT` -> `FW_ERR_TIMEOUT`; `HAL_BUSY` -> `FW_ERR_BUSY`; `HAL_ERROR` -> `FW_ERR_IO` (read `HAL_xxx_GetError()` when the cause matters) |
| Zephyr (negative errno) | `0` | `-ETIMEDOUT`, `-EAGAIN` -> `FW_ERR_TIMEOUT`; `-EBUSY` -> `FW_ERR_BUSY`; `-EINVAL` -> `FW_ERR_INVALID_ARG`; `-ENOMEM` -> `FW_ERR_NO_MEM`; `-ENOTSUP`, `-ENOSYS` -> `FW_ERR_NOT_SUPPORTED`; others -> `FW_ERR_IO` |
| MCUXpresso `status_t` | `kStatus_Success` | `kStatus_Timeout` -> `FW_ERR_TIMEOUT`; `kStatus_InvalidArgument` -> `FW_ERR_INVALID_ARG`; `kStatus_Busy` / `kStatus_LPI2C_Busy` -> `FW_ERR_BUSY`; `kStatus_Fail`, NACK -> `FW_ERR_IO` |
| FX3 `CyU3PReturnStatus_t` | `CY_U3P_SUCCESS` | `CY_U3P_ERROR_TIMEOUT` -> `FW_ERR_TIMEOUT`; `CY_U3P_ERROR_BAD_ARGUMENT` -> `FW_ERR_INVALID_ARG`; `CY_U3P_ERROR_MEMORY_ERROR` -> `FW_ERR_NO_MEM`; others -> `FW_ERR_IO` |

Keep the mapping in one `static` function per port and log the original code at the point
of mapping (once), so the vendor detail is not lost. Confirm exact constant names against
the SDK version in the project; names above are the common ones.

## OSAL: operating-system abstraction

Use it only for modules that must run on more than one RTOS, or on a PC (tests), or both
with and without an RTOS. A product on one RTOS can call the RTOS directly in app and in
ports; the layering rule then is "RTOS calls only in app, port, and osal".

```c
/* osal.h - what portable modules may ask of the OS */
#include "osal_port_types.h"      /* provided per port: defines osal_mutex_t, osal_queue_t, osal_sem_t */

fw_status_t osal_mutex_init(osal_mutex_t *m);
fw_status_t osal_mutex_lock(osal_mutex_t *m, uint32_t timeout_ms);
void        osal_mutex_unlock(osal_mutex_t *m);

fw_status_t osal_sem_init(osal_sem_t *s, uint32_t initial, uint32_t max);
fw_status_t osal_sem_take(osal_sem_t *s, uint32_t timeout_ms);
fw_status_t osal_sem_give(osal_sem_t *s);
fw_status_t osal_sem_give_from_isr(osal_sem_t *s);

fw_status_t osal_queue_init(osal_queue_t *q, void *storage, size_t item_size, size_t depth);
fw_status_t osal_queue_send(osal_queue_t *q, const void *item, uint32_t timeout_ms);
fw_status_t osal_queue_send_from_isr(osal_queue_t *q, const void *item);
fw_status_t osal_queue_receive(osal_queue_t *q, void *item, uint32_t timeout_ms);

void        osal_delay_ms(uint32_t ms);
uint32_t    osal_now_ms(void);

#define OSAL_WAIT_FOREVER 0xFFFFFFFFu
```

- **Static allocation**: `osal_port_types.h` defines the types so that the object embeds
  the RTOS's storage (`StaticSemaphore_t`, `StaticQueue_t`, `struct k_mutex`, `TX_MUTEX`).
  Each port ships its own `osal_port_types.h`, selected by the include path. Do not hide
  storage behind `malloc`.
- **Bare-metal build** (`osal_none.c`): mutexes are no-ops or interrupt masking, delays
  spin on the tick; this lets the same modules run in a super-loop and in tests.
- **A ready-made OSAL exists**: CMSIS-RTOS2 (`cmsis_os2.h`) is implemented for FreeRTOS,
  ThreadX, and RTX and is already in the STM32 and NXP SDKs; Zephyr and ESP-IDF have their
  own APIs plus POSIX-like layers. Wrap whichever you pick behind `osal.h` rather than
  letting its types into driver headers.
- **`_from_isr` variants** are separate functions because the underlying RTOS requires it;
  never let one function guess its context.

## Time and timeouts

- A single monotonic millisecond clock (`uint32_t`, wraps in 49.7 days). Compare with
  subtraction, never `now > deadline`:

```c
static bool deadline_passed(uint32_t now_ms, uint32_t deadline_ms)
{
    return (int32_t)(now_ms - deadline_ms) >= 0;   /* correct across wrap-around */
}
```

- Every wait has a limit, and the limit is a parameter of the call or a named constant
  with its source (datasheet figure + margin), never a bare `1000`.
- Deadlines (`start + timeout`) are computed once; loops re-check `deadline_passed`, so
  waiting stays bounded when something else delays the task.
- A fake clock in tests (`hal_clock_t` with a settable `now_ms`) makes timeout paths
  testable in microseconds.

## When to skip the abstraction

- A GPIO toggled once at boot for an LED: call the SDK in board code.
- A peripheral used only by a port-specific feature (ESP32 Wi-Fi provisioning, nRF
  Bluetooth connection parameters): wrap it at the **service** level
  (`svc_wifi`, `svc_ble`) with your own interface, not as a `hal_wifi`.
- An SDK whose own interface is already the abstraction you want (Zephyr's device API,
  CMSIS-Driver): use it directly in drivers and fake it in tests with its own mechanisms
  (Zephyr emulators, `native_sim`) or with a thin adapter.
- A hot path where the indirect call is measurable: bind at link time or with `static
  inline` for that path only.
