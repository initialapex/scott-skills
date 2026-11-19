# Nordic nRF with the nRF Connect SDK (Zephyr)

Contents: Zephyr already has the pattern · how the layers map · app layout, devicetree,
Kconfig · ports over the device API · writing a driver: Zephyr or application · threads,
queues, work · GPIO callbacks · Bluetooth · power, storage, DFU · testing · pitfalls

Scope: nRF52/nRF53/nRF54/nRF91 with the nRF Connect SDK (NCS), which is Zephyr plus
Nordic's modules, built with `west`. The legacy nRF5 SDK (SoftDevice, `nrf_drv_*`) is not
covered; the layering ideas still apply. Version-sensitive names (board qualifiers,
Bluetooth macros) are flagged; confirm them against the NCS version of the project.

## Zephyr already has the pattern

Zephyr's driver model is struct + function pointers in C:

- `struct device` holds `name`, a `const void *config` (ROM: addresses, pins), a
  `void *data` (RAM: runtime state), and `const void *api` (a **const table of function
  pointers** per device class: `struct i2c_driver_api`, `struct sensor_driver_api`, ...).
- `DEVICE_DT_INST_DEFINE()` instantiates one device per devicetree node, binding config,
  data, and api at build time.
- Application code calls `i2c_write_read(dev, ...)`, which dispatches through `dev->api`.

That is the same split this skill uses (`ops` in flash, `self` in RAM), and Zephyr's own
`CONTAINER_OF(ptr, type, member)` and embedded `struct k_work` / `struct gpio_callback` are
the first-member idiom. So **the Zephyr device API is the HAL layer**: you do not need a
`hal_i2c` wrapper unless you want PC unit tests without Zephyr, or portability to
another SDK. When you do, the port is a few lines (below).

## How the layers map

| Layer | In an NCS project |
|-------|-------------------|
| app | `src/main.c`, app threads, composition of objects (`board.c` / `app_init.c`) |
| service | library in the app (`src/services/`) or a Zephyr module (`zephyr_library()`) |
| driver | a Zephyr driver (reusable, devicetree-bound) or an application driver with our ops (see below) |
| hal | the Zephyr device API (`i2c.h`, `gpio.h`, `spi.h`, `uart.h`, `sensor.h`), or our `hal_*` interfaces when portability or host tests need them |
| port | adapters from our interfaces to Zephyr calls (`port_zephyr_i2c.c`); nrfx drivers for features Zephyr does not expose |
| board | devicetree overlay + `boards/*.conf`: pins, buses, which device is which |

Vendor-include rule here: `<zephyr/...>` and `nrfx_*.h` only in port, board, app glue,
and Zephyr drivers; drivers and services written against our interfaces stay free of
them.

## App layout, devicetree, and Kconfig

```
my_app/
  CMakeLists.txt          find_package(Zephyr ...), target_sources(app PRIVATE ...)
  prj.conf                CONFIG_*=y: what to build in
  Kconfig                 your own options (source "Kconfig.zephyr" at the end)
  boards/nrf52840dk_nrf52840.overlay   devicetree additions for the board
  boards/nrf52840dk_nrf52840.conf      board-specific Kconfig
  src/                    app, services, drivers, ports
  tests/                  ztest suites (native_sim)
```

- **Devicetree describes hardware**: which pins, which bus, which chip at which address.
  An overlay adds the sensor to the board:

```dts
&i2c0 {
    status = "okay";
    tmp102: tmp102@48 {
        compatible = "i2c-device";    /* or a real binding if one exists */
        reg = <0x48>;
    };
};
```

  The code then asks for the node, never a hard-coded pin or address:
  `static const struct i2c_dt_spec s_tmp = I2C_DT_SPEC_GET(DT_NODELABEL(tmp102));`.
- **Kconfig selects software**: `prj.conf` has `CONFIG_I2C=y`, `CONFIG_GPIO=y`,
  `CONFIG_LOG=y`, `CONFIG_ASSERT=y`, stack sizes, buffer sizes, and your own module
  options. Define your module's symbols in a `Kconfig` file and compile its sources with
  `target_sources_ifdef(CONFIG_MY_THERMAL app PRIVATE ...)` (or a `zephyr_library()` in a
  module). Pass the resulting numbers into objects through config structs, so a module
  does not read `CONFIG_` itself.
- **Board names** use qualifiers in current releases (`west build -b
  nrf52840dk/nrf52840`); older releases use `nrf52840dk_nrf52840`. Use `west build -p
  always` after changing overlays or Kconfig fragments if the build looks stale.
- Zephyr's APIs report errors as **negative errno** (`0` = success): map them in the port
  ([hal-osal-ports.md](hal-osal-ports.md)).

## A port over the device API

```c
/* port_zephyr_i2c.c - adapts one Zephyr I2C bus to hal_i2c_t */
#include <zephyr/drivers/i2c.h>
#include "hal_i2c.h"

typedef struct port_zephyr_i2c {
    hal_i2c_t base;                 /* first member */
    const struct device *bus;       /* from DEVICE_DT_GET(DT_NODELABEL(i2c0)) */
} port_zephyr_i2c_t;

static fw_status_t map_errno(int rc)
{
    switch (rc) {
    case 0:          return FW_OK;
    case -ETIMEDOUT: return FW_ERR_TIMEOUT;
    case -EBUSY:     return FW_ERR_BUSY;
    case -EINVAL:    return FW_ERR_INVALID_ARG;
    case -ENOMEM:    return FW_ERR_NO_MEM;
    case -ENOTSUP:   return FW_ERR_NOT_SUPPORTED;
    default:         return FW_ERR_IO;      /* -EIO covers NACK and bus errors */
    }
}

static fw_status_t port_write_read(hal_i2c_t *base, uint8_t addr7, const uint8_t *tx, size_t tx_len,
                                   uint8_t *rx, size_t rx_len, uint32_t timeout_ms)
{
    port_zephyr_i2c_t *self = FW_CONTAINER_OF(base, port_zephyr_i2c_t, base);

    FW_UNUSED(timeout_ms);   /* the Zephyr I2C API has no per-call timeout; see the driver config */
    return map_errno(i2c_write_read(self->bus, addr7, tx, tx_len, rx, rx_len));
}
```

`i2c_write_read()` takes the 7-bit address and serializes access to the bus inside the
driver. Because the call has no timeout parameter, say so in the port: the bound is the
driver's own transfer timeout, not `timeout_ms`; if the interface contract requires the
bound, run the transfer in a work item or give the bus driver a timeout option. Check
`device_is_ready(bus)` once at init and return `FW_ERR_INVALID_STATE` if not.

If the project is Zephyr-only and has no host tests of the drivers, skip the port and
let the driver take an `struct i2c_dt_spec` directly (`i2c_write_read_dt(&spec, ...)`).

## Driver: a Zephyr driver, or an application driver?

| Choose | When |
|--------|------|
| **Zephyr driver** (`DT_DRV_COMPAT`, devicetree binding yaml, a standard API such as `sensor`) | the chip will be reused across applications, or the SDK already defines its class; the app then uses `sensor_sample_fetch` / `sensor_channel_get` |
| **Application driver with our ops** (as in `templates/`) | one product, custom interface, host-testable without Zephyr, or portable to a non-Zephyr SDK |

Check `drivers/` of the SDK before writing either: many sensors already have a Zephyr
driver. A Zephyr driver follows the same rules: `config` (ROM), `data` (RAM), an `api`
const table, an init function returning negative errno, no global state.

## Threads, queues, and work

- **Threads**: `K_THREAD_DEFINE(name, stack_size, entry, p1, p2, p3, priority, options,
  delay)` defines a static thread and stack. A **lower number means higher priority**;
  negative priorities are cooperative. Size stacks from `CONFIG_THREAD_ANALYZER` or
  `k_thread_stack_space_get()`.
- **Message queue**: `K_MSGQ_DEFINE(q, sizeof(struct msg), depth, alignment)`;
  `k_msgq_put(&q, &msg, K_NO_WAIT)` is the form that may be used from an ISR; consumers
  use `k_msgq_get(&q, &msg, K_FOREVER)`.
- **Work queue**: `k_work_init(&w, handler)` + `k_work_submit(&w)` defers a function to
  the system work queue thread; `k_work_delayable` + `k_work_schedule(&dw, K_MSEC(n))`
  for timers. The handler receives `struct k_work *`; recover the owner with
  `CONTAINER_OF(work, owner_t, work)`, with the `k_work` embedded in your object. Work
  handlers must not block for long: they stall the whole queue (use a dedicated
  `k_work_q` for slow work).
- **Timers**: `k_timer` expiry functions run in ISR context; use them only to submit work.
- **Synchronization**: `k_mutex` (priority inheritance), `k_sem`, `k_event`, and **zbus**
  for publish/subscribe between services (matches "events go up").
- **Time**: `k_uptime_get_32()` for the monotonic millisecond clock (wraps), `k_sleep(K_MSEC(n))`
  to block; `K_MSEC`, `K_FOREVER`, `K_NO_WAIT` for timeouts.

## GPIO callbacks

```c
typedef struct button {
    struct gpio_callback cb_data;      /* embedded: the callback finds the owner with CONTAINER_OF */
    struct gpio_dt_spec pin;
    button_cb_t on_event;
    void *ctx;
} button_t;

static void on_gpio(const struct device *port, struct gpio_callback *cb, gpio_port_pins_t pins)
{
    button_t *self = CONTAINER_OF(cb, button_t, cb_data);     /* ISR context */
    FW_UNUSED(port); FW_UNUSED(pins);
    self->on_event(self->ctx, BUTTON_PRESSED);                /* contract: ISR-safe or post a work item */
}

/* init: gpio_pin_configure_dt(&pin, GPIO_INPUT); gpio_pin_interrupt_configure_dt(&pin,
 * GPIO_INT_EDGE_TO_ACTIVE); gpio_init_callback(&b->cb_data, on_gpio, BIT(pin.pin));
 * gpio_add_callback(pin.port, &b->cb_data); */
```

## Bluetooth LE

- `bt_enable(NULL)` starts the stack (blocking) or `bt_enable(ready_cb)` for async;
  connection events through `BT_CONN_CB_DEFINE(...)` (static) or `bt_conn_cb_register()`;
  GATT services with `BT_GATT_SERVICE_DEFINE`; notify with `bt_gatt_notify()`; advertising
  with `bt_le_adv_start()`. Macro and parameter names changed between releases (for
  example `BT_LE_ADV_CONN_NAME` is deprecated); use the sample of your NCS version as the
  reference.
- Callbacks run in the Bluetooth RX/system work queue threads: keep them short, **copy the
  data out and post a message** to your service's queue; do not take long locks or block.
- Wrap Bluetooth as a **service** with your own interface (`svc_ble`: start advertising,
  connected/disconnected events, `send()`), as with Wi-Fi on ESP32. A `hal_ble` is the wrong
  layer.
- Multi-core parts (nRF53) run the controller on the network core; the build is
  multi-image (sysbuild) and cross-core messages use IPC; keep what runs on each core
  in its own application folder.

## Power, storage, DFU

- Idle power is automatic in System ON; measure with the Power Profiler, and turn off
  what you do not use (`CONFIG_SERIAL=n`, `CONFIG_LOG` modes, unused peripherals
  `status = "disabled"`). Device power management is `CONFIG_PM_DEVICE` with
  `pm_device_action_run()`.
- **Settings and NVS** (`CONFIG_SETTINGS`, `settings_load()`, `CONFIG_NVS`/the newer storage
  backends) behind a `svc_settings` service with a version and defaults
  ([robustness.md](robustness.md)).
- **MCUboot** for DFU (`CONFIG_BOOTLOADER_MCUBOOT`, sysbuild): confirm the new image only
  after a self-test (`boot_write_img_confirmed()`), so a bad image rolls back.
- `nrfx` drivers give access to hardware features Zephyr does not model (PPI/DPPI,
  specific timers): wrap them as a port and keep `nrfx_*.h` out of upper layers.

## Testing

- **ztest on `native_sim`**: the Zephyr application runs as a Linux process.
  `ZTEST_SUITE(name, NULL, NULL, NULL, NULL, NULL)`, `ZTEST(name, test_foo) { zassert_equal(...); }`,
  run with `west twister -p native_sim -T tests` or `west build -b native_sim` and execute
  the binary. Use the I2C/SPI/GPIO **emulators** (`zephyr,emul` devicetree nodes) or the
  fake drivers to script a device; Zephyr's FFF (`DEFINE_FAKE_VALUE_FUNC`) fakes single functions.
- The dependency-free approach in [testing-host.md](testing-host.md) works for the drivers
  and services written against our interfaces; both can coexist (host build for logic,
  `native_sim` for Zephyr integration).
- On target: ztest runs on real boards (`west twister -p nrf52840dk/nrf52840 --device-testing`) for
  port-contract and loopback tests.

## Pitfalls

- Hard-coding a pin or address in C instead of devicetree.
- Calling blocking APIs (`k_mutex_lock`, `i2c_*`, `k_msgq_put` with a timeout) from an ISR or
  a `k_timer` callback.
- Long work in a system work queue handler, starving other work items.
- Stack sizes copied from a sample (too small for `LOG` formatting or Bluetooth callbacks);
  `CONFIG_STACK_SIZE`-related crashes appear as hard faults in unrelated code.
- Ignoring negative errno returns; mixing errno values and `fw_status_t` across layers.
- Forgetting `device_is_ready()`; the device exists but its init failed.
- A Kconfig option set in `prj.conf` that a dependency silently overrides (read the
  build log warnings: "was assigned the value y but got the value n").
- Using `printk`/`LOG` heavy output in a latency-critical path or leaving `CONFIG_SERIAL`
  enabled in a low-power product.
