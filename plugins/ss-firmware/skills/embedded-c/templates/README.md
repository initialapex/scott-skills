# Templates

A small firmware slice that follows the skill's rules and builds on a PC. Copy the files
you need, rename the module, and adapt the status codes and names to the project. All of
it compiles with gcc, clang (`-std=c99 -Wall -Wextra -Wpedantic -Werror`), and MSVC
(`/std:c11 /W4 /WX`).

| File | Layer | What it shows |
|------|-------|---------------|
| `common/fw_common.h` | common | `fw_status_t`, `FW_CONTAINER_OF`, `FW_STATIC_ASSERT`, big-endian helpers; the only header every layer shares |
| `hal/hal_i2c.h` | HAL | An interface: base struct + const ops table + checking wrappers; 7-bit address and timeout conventions a port must follow |
| `drivers/sensor.h` | driver | A second, higher interface (what a service consumes), independent of any chip |
| `drivers/drv_tmp102.[ch]` | driver | A device driver as an object: embeds `sensor_t` as its first member, receives its bus by injection, `init(self, cfg)`, `container_of` in the method, const ops table in flash |
| `services/svc_thermal_monitor.[ch]` | service | Logic with no hardware and no thread: injected sensor, callback + context, explicit state enum, hysteresis, fault counting |
| `tests/fw_test.[ch]` | test | A dependency-free harness; swap it for Unity or ztest |
| `tests/mock_i2c.[ch]` | test | A test double built from the same ops table as a real port: scripted status and data, recorded arguments |
| `tests/test_tmp102.c`, `tests/test_thermal_monitor.c` | test | Driver tested against the mock bus, service tested against a fake sensor |
| `run-tests.ps1`, `run-tests.sh` | tool | Build and run on a PC (PowerShell picks gcc, clang, or MSVC; the shell script adds sanitizers) |

Not included on purpose: a port. A port is five to thirty lines per platform and depends
on the SDK in use; the platform references show one for each SDK.

## Adding a module

1. Header first: the config struct, the object struct (the interface base as the first
   member when the module implements an interface), and the functions.
2. Write the test with the mock before the implementation, as in `test_tmp102.c`: one
   test per rule (argument checks, the conversion, each error path, the call the driver
   makes on the bus).
3. Add the source and the test to both scripts (a `Makefile` or CMake `add_executable`
   does the same job once the project has more than a few files).
4. Wire the real objects together in one place on the target (the composition root, see
   `references/architecture-layers.md`).
