# Host-side unit tests

Contents: what to test where · test doubles built from ops tables · fake clock ·
structure of a test · frameworks per SDK · building on a PC · hardware-in-the-loop

The layering exists so that most firmware logic runs on a PC in milliseconds, with no
board, no flashing, and no vendor SDK. `templates/` holds a working example
(`run-tests.ps1` / `run-tests.sh`); this file explains how to extend it.

## What to test where

| Test | Runs on | Covers | Needs |
|------|---------|--------|-------|
| **Host unit** | PC | drivers (against a mock bus), services, state machines, protocol parsers, ring buffers, CRC, conversions, command tables | interfaces and mocks; no vendor headers |
| **Target unit / integration** | the MCU (Unity on ESP-IDF, ztest on Zephyr, a test firmware) | ports, DMA, interrupts, timing, memory budgets, the real vendor driver behavior | a board |
| **Hardware-in-the-loop** | board + fixture | end-to-end behavior: a known device on the bus, loopback UART, signal generator | rig and scripts |

Aim for the host layer to hold the logic, so that the target layer only has to prove the
port keeps the interface contract (units, error mapping, timeouts, locking).

## Test doubles from ops tables

An interface is a struct of function pointers, so a test double is another ops table. The
four useful kinds:

- **Stub**: returns scripted values (`next_status`, a `rx_data` buffer). Simplest; most
  tests need only this. (`mock_i2c_t` in the template.)
- **Spy/mock**: also records what it was called with (`last_addr7`, `last_tx`,
  `call_count`) so the test asserts the driver's bus traffic.
- **Fake**: a small working implementation (an in-RAM flash with erase and write
  rules, a loopback UART, a bus with a simulated device register file). Best for services
  that exercise many calls (a storage service on a fake flash catches wear and power-fail
  bugs).
- **Strict expectation script**: a queue of expected transactions; the double fails the test
  when the driver deviates:

```c
typedef struct i2c_step {
    uint8_t addr7;
    uint8_t tx[4]; size_t tx_len;          /* what the driver must send */
    uint8_t rx[4]; size_t rx_len;          /* what the device answers */
    fw_status_t result;
} i2c_step_t;

typedef struct strict_i2c {
    hal_i2c_t base;
    const i2c_step_t *steps; size_t count; size_t next;
    unsigned mismatches;
} strict_i2c_t;
/* write_read(): compare with steps[next], count a mismatch if different, copy rx, next++ */
/* in the test: ASSERT(strict.next == strict.count && strict.mismatches == 0) */
```

Use a strict script to pin down a **device init sequence** (register writes in order,
datasheet-mandated delays), and a plain stub for ordinary behavior.

- Prefer **stubs and fakes** over heavy mock frameworks; they survive refactoring because
  they model the device, not the call order.
- Put doubles in `tests/` (shared ones in `tests/doubles/`) and keep them as small as the
  interface; if a double is hard to write, the interface is too big.
- For link-time seams (no ops table), link a fake `.c` that defines the same functions
  (`fake_gpio.c` instead of `port_stm32_gpio.c`).

## Fake clock

Time makes tests slow and flaky. Inject the clock (`hal_clock_t`) and give tests a fake
whose `now_ms` they set:

```c
typedef struct fake_clock { hal_clock_t base; uint32_t now_ms; } fake_clock_t;
static uint32_t fake_now(hal_clock_t *b) { return FW_CONTAINER_OF(b, fake_clock_t, base)->now_ms; }

/* In the test: set clock.now_ms = 0xFFFFFFF0u, start the operation, then
 * clock.now_ms += 0x20u, which crosses the 32-bit wrap, and check the timeout fires. */
```

Test timeouts by advancing the clock past the deadline (and across the 32-bit wrap), not by
sleeping. For blocking calls inside the code under test (`osal_delay_ms`), the host OSAL
(`osal_none.c`) advances the fake clock instead of sleeping.

## Structure of a test

- **One rule per test**, named for the behavior: `test_over_temp_is_reported_once`, not
  `test_poll_2`. A failure message should tell the reader what broke.
- **Arrange / act / assert** with a `setup()` that builds the object on the doubles.
- **Cover, for every module:** argument validation (`NULL`, zero length, out of range),
  the normal path, **each error path** (a double returning each status), boundaries (full,
  empty, max length, wrap-around, first and last value), and state transitions (every
  edge of the state machine, including the ones that must do nothing).
- **Table-driven** for conversions and parsers: an array of `{input, expected}` taken from
  the datasheet examples or captured frames; add a failing capture as a regression row
  first, then fix.
- **Do not** test the SDK, sleep, depend on test order, share state between tests, or reach
  into private statics (when unavoidable, `#include "foo.c"` in the test file rather than
  widening the public API; use sparingly).
- Run under **sanitizers** (`-fsanitize=address,undefined`) on Linux/macOS/MinGW-w64
  builds; they catch off-by-one and shift bugs for free.

## Frameworks per SDK

| Context | Suggested framework | Notes |
|---------|---------------------|-------|
| Plain C, any SDK | the dependency-free harness in `templates/tests/`, or **Unity** (+ **CMock** / **Ceedling** for generated mocks) | Unity is small and builds with any compiler; Ceedling generates mocks from headers if the team wants them |
| ESP-IDF | Unity is built in: `unity_test` components run on the target; pure-C components can also be built for the host with the `linux` target (`idf.py --preview set-target linux`) or compiled by a separate CMake project | host-build only components without hardware dependencies |
| Zephyr / nRF Connect SDK | **ztest** with the `native_sim` board (the app runs as a Linux process), plus Zephyr's emulators and fake drivers for I2C/SPI/GPIO; run with `west twister` | the device model is itself an ops table, so fakes plug in by devicetree |
| STM32 / NXP / generic CMake | a host CMake project that compiles `drivers/`, `services/`, `common/`, and `tests/` for the PC, with `ctest` | keep it separate from the target build so no vendor path leaks in |
| FX2 (8051) | host-compile the portable logic (parsers, command tables) with the PC compiler behind small `#define` shims for `__xdata` and friends; SDCC has a simulator (`ucsim`) | see [platform-fx2.md](platform-fx2.md) |
| C++ test tools (GoogleTest) | allowed for C code: headers carry `extern "C"` | useful when the company already uses it for software tests |

## Building on a PC

```cmake
# tests/CMakeLists.txt - a host-only project; no vendor SDK is referenced
cmake_minimum_required(VERSION 3.16)
project(fw_host_tests C)
set(CMAKE_C_STANDARD 99)
set(CMAKE_C_STANDARD_REQUIRED ON)

add_executable(fw_tests
    ../drivers/drv_tmp102.c
    ../services/svc_thermal_monitor.c
    doubles/mock_i2c.c
    test_tmp102.c test_thermal_monitor.c test_main.c)
target_include_directories(fw_tests PRIVATE ../common ../hal ../drivers ../services doubles)
target_compile_options(fw_tests PRIVATE
    $<$<C_COMPILER_ID:GNU,Clang>:-Wall -Wextra -Wpedantic -Werror>
    $<$<C_COMPILER_ID:MSVC>:/W4 /WX>)

enable_testing()
add_test(NAME fw_tests COMMAND fw_tests)
```

- Compile **the same source files** the target compiles; do not copy them. A tiny
  `host_config.h` (or compiler defines) maps `__xdata`, `IRAM_ATTR`, and similar
  annotations to nothing when a file needs them.
- Run it in CI on every commit; it takes seconds. Add `gcov`/`lcov` (or `llvm-cov`) to see
  which error paths are never exercised; chase the untested branches in drivers first.
- If the PC and the MCU differ in `int` size, endianness, or alignment rules, the tests
  also protect against wrong assumptions: keep a 32-bit build (`-m32`) in the matrix when
  the target has a 16-bit `int` (AVR, 8051) or pointer-size assumptions matter.

## Hardware-in-the-loop

Ports and timing need the board. Keep a small set of repeatable checks that run on target
and report pass/fail on a serial port or in the test framework's output:

- GPIO loopback (two pins wired together) for the GPIO port: write, read, edge callback
  count and latency.
- UART loopback and a known baud rate for the UART port: throughput, no loss at a
  sustained rate, overflow counted, idle-line behavior.
- A known I2C/SPI device (an EEPROM or a sensor on the bench board) for the bus ports:
  a read and write round trip, a deliberately wrong address returns `FW_ERR_IO`, a held bus
  returns `FW_ERR_TIMEOUT`.
- A stress run (hours) with maximum interrupt load, watching the stack high-water mark,
  heap low-water mark, and drop counters.

Record the results with the build id, so a regression names its first bad build.
