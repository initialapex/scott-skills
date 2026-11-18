# ESP32 with ESP-IDF (FreeRTOS)

Contents: how the layers map · components and build · writing a port (I2C, GPIO) ·
`esp_err_t` · tasks and events · interrupts and IRAM · memory · storage, Wi-Fi, USB ·
testing · pitfalls

Scope: ESP-IDF 5.x in C (ESP32, S2, S3, C3, C6, H2 families). Not the Arduino core. API
names below are for IDF 5.x; where a driver changed between releases the text says so.
Confirm the version in `idf.py --version` and the API reference of that version.

## How the layers map

| Layer | In an ESP-IDF project |
|-------|----------------------|
| app | `main/` (component `main`), `app_main()` creates objects and tasks; it runs in the main task |
| service | its own component per service (`svc_thermal`, `svc_wifi`) |
| driver | its own component per chip (`drv_tmp102`), `REQUIRES hal common` |
| hal | a header-only component (`hal`), interfaces only |
| port | one component (`port_esp32`) with `PRIV_REQUIRES` on the IDF drivers it wraps |
| osal | `osal_freertos` in the same port component; IDF is FreeRTOS, so it is thin |
| board | `main/board_<name>.c` or a `board` component with the pin map |

`REQUIRES` / `PRIV_REQUIRES` in each component's `CMakeLists.txt` **is** the include rule:
a driver component that does not list `esp_driver_i2c` cannot include its headers, so
the build enforces layering.

```cmake
# components/drv_tmp102/CMakeLists.txt
idf_component_register(
    SRCS "drv_tmp102.c"
    INCLUDE_DIRS "include"
    REQUIRES hal common          # note: no IDF driver here
)

# components/port_esp32/CMakeLists.txt
idf_component_register(
    SRCS "port_esp32_i2c.c" "port_esp32_gpio.c" "osal_freertos.c"
    INCLUDE_DIRS "include"
    REQUIRES hal common
    PRIV_REQUIRES esp_driver_i2c esp_driver_gpio freertos     # IDF 5.3+; use `driver` on older IDF
)
```

Use `idf_component.yml` (component manager) for third-party components; keep your own
modules local under `components/`. Configuration lives in `Kconfig.projbuild` (menu
entries for pins, intervals, buffer sizes) and `sdkconfig.defaults` (committed);
`sdkconfig` itself is generated and not edited by hand. Read configuration as
`CONFIG_...` macros in board code, and pass values into objects through their config
structs, so the modules stay free of `CONFIG_` references.

## Status: `esp_err_t`

`ESP_OK` is 0. Map in the port with a static function (see
[hal-osal-ports.md](hal-osal-ports.md) for the table). Do not use `ESP_ERROR_CHECK()` in
drivers or services: it aborts the system on any error. Use it only in start-up code in
`main/`, where a failure means the product cannot run, and log recoverable errors with
`ESP_LOGE(TAG, "...: %s", esp_err_to_name(err))`.

## A port: I2C

IDF 5.x provides the "new" master driver (`driver/i2c_master.h`): create a bus once, add
one device handle per address, then call transmit/receive on the device handle. The older
`driver/i2c.h` (`i2c_master_write_read_device`) still exists but is deprecated in recent
releases. Because the device handle is per address and our HAL takes the address on each
call, the port caches handles:

```c
/* port_esp32_i2c.c */
#include "driver/i2c_master.h"
#include "esp_log.h"
#include "hal_i2c.h"

#define PORT_I2C_MAX_DEVICES 4u
static const char *TAG = "port_i2c";

typedef struct port_esp32_i2c {
    hal_i2c_t base;                              /* first member */
    i2c_master_bus_handle_t bus;
    uint32_t scl_speed_hz;
    struct { uint8_t addr7; i2c_master_dev_handle_t dev; } devs[PORT_I2C_MAX_DEVICES];
    size_t dev_count;
    osal_mutex_t lock;                           /* serializes transactions; see thread-safety note */
} port_esp32_i2c_t;

static fw_status_t map_err(esp_err_t e)
{
    switch (e) {
    case ESP_OK:                  return FW_OK;
    case ESP_ERR_TIMEOUT:         return FW_ERR_TIMEOUT;
    case ESP_ERR_INVALID_ARG:     return FW_ERR_INVALID_ARG;
    case ESP_ERR_INVALID_STATE:   return FW_ERR_INVALID_STATE;
    case ESP_ERR_NO_MEM:          return FW_ERR_NO_MEM;
    default:                      return FW_ERR_IO;       /* NACK and bus errors: the exact code depends on the IDF version; confirm */
    }
}

/* Find or create the device handle for an address. Called with the lock held. */
static fw_status_t get_dev(port_esp32_i2c_t *self, uint8_t addr7, i2c_master_dev_handle_t *out)
{
    size_t i;
    i2c_device_config_t cfg = {
        .dev_addr_length = I2C_ADDR_BIT_LEN_7,
        .device_address = addr7,
        .scl_speed_hz = self->scl_speed_hz,
    };

    for (i = 0u; i < self->dev_count; i++) {
        if (self->devs[i].addr7 == addr7) { *out = self->devs[i].dev; return FW_OK; }
    }
    if (self->dev_count >= PORT_I2C_MAX_DEVICES) { return FW_ERR_NO_MEM; }
    if (i2c_master_bus_add_device(self->bus, &cfg, &self->devs[self->dev_count].dev) != ESP_OK) {
        return FW_ERR_IO;
    }
    self->devs[self->dev_count].addr7 = addr7;
    *out = self->devs[self->dev_count++].dev;
    return FW_OK;
}

static fw_status_t port_write_read(hal_i2c_t *base, uint8_t addr7, const uint8_t *tx, size_t tx_len,
                                   uint8_t *rx, size_t rx_len, uint32_t timeout_ms)
{
    port_esp32_i2c_t *self = FW_CONTAINER_OF(base, port_esp32_i2c_t, base);
    i2c_master_dev_handle_t dev;
    fw_status_t st;

    if (osal_mutex_lock(&self->lock, timeout_ms) != FW_OK) { return FW_ERR_BUSY; }
    st = get_dev(self, addr7, &dev);
    if (st == FW_OK) {
        st = map_err(i2c_master_transmit_receive(dev, tx, tx_len, rx, rx_len, (int)timeout_ms));
        if (st != FW_OK) { ESP_LOGW(TAG, "addr 0x%02x: %d", addr7, (int)st); }
    }
    osal_mutex_unlock(&self->lock);
    return st;
}
```

Create the bus in `port_esp32_i2c_init` with `i2c_new_master_bus()` using an
`i2c_master_bus_config_t` (`i2c_port`, `sda_io_num`, `scl_io_num`, `clk_source =
I2C_CLK_SRC_DEFAULT`, `glitch_ignore_cnt`, `flags.enable_internal_pullup`); external
pull-ups are better than the internal ones for anything beyond a short bench wire.
Check the thread-safety section of the I2C driver documentation for your IDF version;
keep the port mutex unless it states the call is safe from several tasks.

SPI follows the same shape: `spi_bus_initialize()` once, `spi_bus_add_device()` per
chip-select (so a `hal_spi_device_t` maps to one `spi_device_handle_t`), and
`spi_device_transmit()` for blocking transfers; DMA buffers must be in DMA-capable memory
(`DMA_ATTR` or `heap_caps_malloc(..., MALLOC_CAP_DMA)` once at start-up).

## A port: GPIO and interrupts

`gpio_install_isr_service()` is called once; `gpio_isr_handler_add(pin, handler, arg)`
already takes a context pointer, which matches `hal_gpio_cb_t(void *ctx)`:

```c
static void IRAM_ATTR gpio_isr(void *arg)               /* ISR: IRAM, no blocking, no logging */
{
    port_esp32_gpio_t *self = (port_esp32_gpio_t *)arg;

    if (self->cb != NULL) { self->cb(self->ctx); }     /* the HAL contract says: ISR context */
}
```

The HAL header must say the callback runs in ISR context; the service layer's callback
only posts to a queue (`xQueueSendFromISR`) and returns. Use `portYIELD_FROM_ISR()` as
the SDK documents for the version in use.

## Tasks, queues, and events

- `app_main()` is a normal task (stack size `CONFIG_ESP_MAIN_TASK_STACK_SIZE`); it may
  create objects and tasks and then return.
- `xTaskCreate()` takes the stack size in **bytes** in ESP-IDF (not words as in vanilla
  FreeRTOS). On dual-core chips `xTaskCreatePinnedToCore()` pins a task to a core; leave
  tasks unpinned unless a driver requires it (Wi-Fi and Bluetooth run on core 0 by
  default). Use `xTaskCreateStatic()` with static `StackType_t` buffers to avoid heap use.
- **One task per concern, communicating by queues**, matches the layers: the sensor task
  calls `thermal_monitor_poll()` every N ms (`vTaskDelayUntil`), the callback posts an event
  to the app queue, the app task reacts.
- **`esp_event`** (default or custom loop) is the idiomatic "events go up" channel for
  services: `esp_event_handler_register()`, `esp_event_post()`, with event IDs declared per
  service (`ESP_EVENT_DECLARE_BASE` / `ESP_EVENT_DEFINE_BASE`). Handlers run in the loop's
  task: keep them short.
- Timers: `esp_timer` (high resolution, callbacks in the `esp_timer` task) for periodic
  work; FreeRTOS software timers for coarse ones. Callbacks must not block.
- Watchdogs: the task watchdog (`esp_task_wdt_*`) is the supervisor mechanism; subscribe
  each critical task and feed from the task's loop, not from a timer.

## Interrupts, IRAM, and flash

- While flash is being written or erased, the cache can be disabled; an ISR registered
  **without** `ESP_INTR_FLAG_IRAM` is not run during that time, and one **with** it must
  live entirely in IRAM and touch only DRAM data. This includes `const` tables: a `static
  const` ops table sits in flash (rodata) and **cannot** be read from an IRAM-safe ISR
  while the cache is off. Mark such a table `DRAM_ATTR`, or call a plain function instead
  of going through an ops table in that handler.
- Use `ESP_INTR_FLAG_IRAM` only when the interrupt must be serviced during flash
  operations (such as a time-critical peripheral); otherwise let it be delayed.
- Critical sections across both cores use a spinlock: `portMUX_TYPE` with
  `portENTER_CRITICAL(&lock)` (see [interrupts-concurrency.md](interrupts-concurrency.md)).

## Memory

- Internal RAM is split (IRAM/DRAM, DMA-capable, RTC); `heap_caps_*` APIs select by
  capability. Allocate DMA buffers and long-lived objects at start-up; log
  `heap_caps_get_free_size()` and `heap_caps_get_minimum_free_size()` to see the low-water
  mark.
- Wi-Fi, Bluetooth, and TLS allocate from the heap at run time; budget for them
  (measure with and without them) and keep your own code static.
- PSRAM (SPIRAM, on some modules) is slower, not DMA-visible on every chip, and not usable
  from IRAM-safe ISRs; keep real-time buffers in internal RAM.
- `idf.py size`, `idf.py size-components`, and `idf.py size-files` give the footprint per
  component; check it as part of the review.

## Storage, Wi-Fi, USB, and OTA

- **NVS** (`nvs_flash_init`, `nvs_open`, `nvs_get_*`/`nvs_set_*`, `nvs_commit`) for settings,
  behind a `svc_settings` service with a version and defaults ([robustness.md](robustness.md)).
- **Wi-Fi/Bluetooth**: wrap them as services with your own interface (connect,
  disconnect, state events), driven by `WIFI_EVENT` / `IP_EVENT` handlers; do not create a
  `hal_wifi`.
- **USB** on S2/S3: the TinyUSB component plays the device-core role in
  [usb-device.md](usb-device.md); descriptors and the vendor/class callbacks are yours.
- **OTA and partitions**: `partitions.csv` and the OTA APIs (`esp_ota_*`) are app-level
  concerns; keep rollback enabled and mark the image valid only after the self-test passes.

## Testing

- Host: build `drivers/`, `services/`, and `common/` with a separate host CMake project
  (as in [testing-host.md](testing-host.md)); the components' sources are the same files.
  The IDF `linux` target (`idf.py --preview set-target linux`) can build some pure
  components with IDF's mocks, within its limits.
- Target: Unity tests as `unity_test` components or a test app, run with `idf.py flash
  monitor` or `pytest-embedded`; use them for the port's contract (timeouts, error mapping)
  and for loopback checks.

## Pitfalls

- Using `ESP_ERROR_CHECK` inside reusable code (aborts instead of returning).
- Logging from an ISR, or calling a non-`IRAM_ATTR` function from an IRAM-safe ISR.
- A `static const` table read by an IRAM-safe ISR (see above).
- Stack sizes in bytes mistaken for words; tasks too small for `ESP_LOGx` formatting or
  Wi-Fi callbacks (check the high-water mark).
- Holding a mutex while calling into the Wi-Fi stack or an `esp_event` post with a long
  timeout.
- Assuming pins are free: strapping pins, flash and PSRAM pins, and USB pins differ per
  chip; take the pin map from the chip's datasheet and the module's schematic.
- Letting `sdkconfig` drift from `sdkconfig.defaults` so a clean checkout builds a
  different firmware.
