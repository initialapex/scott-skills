---
name: embedded-c
description: 'Write, structure, port, and review MCU firmware in C with a layered, function-based architecture (app, service, driver, HAL, port, OSAL) and object-oriented C: structs as objects, const function-pointer tables as interfaces, dependency injection, callbacks with context, state machines, command tables, ISR-to-task hand-off, static allocation, status codes, and host unit tests with mocks. Covers ESP32 (ESP-IDF), STM32 (CubeMX/HAL), Nordic (nRF Connect SDK/Zephyr), NXP (MCUXpresso SDK), and Cypress/Infineon FX2LP (8051) and FX3, including USB device firmware (descriptors, endpoints, vendor requests, DMA, GPIF). Use whenever the user writes, designs, ports, or reviews firmware or embedded C: a peripheral or device driver, a HAL or board layer, an RTOS task or interrupt design, a USB device, or drivers testable without hardware, even if they only say MCU, ISR, bare-metal, ESP32, STM32, nRF52, i.MX RT, FX3, or FX2. Not for Linux kernel drivers, desktop apps, FPGA, PCB design, or cryptography.'
---

# Embedded C: layered firmware with objects in C

Firmware outlives the chip it was first written for, and most bugs hide where hardware
access is mixed into logic. This skill keeps the two apart: **layers** decide who may
call whom, and **objects built from a struct plus a const table of function pointers**
decide how a layer talks to the one below it. The result runs on a PC under a unit test
and ports to another MCU by replacing one layer.

The language is C99 in the spirit of MISRA-C (fixed-width types, no runtime heap, no
recursion, no VLAs), not enforced rule by rule. FX2 (8051) is the exception: its
compilers are C89-like and its memory is tiny, so
[references/platform-fx2.md](references/platform-fx2.md) says what to give up.
Comments in generated code are English, whatever language the conversation uses.

## Step 1: Understand before writing

Firmware mistakes are expensive to find on hardware, so settle these first, reading the
project before asking:

1. **Target**: the exact MCU or module, the SDK and its version, the RTOS (or bare-metal
   super-loop), the toolchain and build system (CMake, west, idf.py, CubeIDE, MCUXpresso,
   Keil, SDCC). Take them from `CMakeLists.txt`, `sdkconfig`, `prj.conf`, `.ioc`,
   `*.mex`, `platformio.ini`, or the existing sources. Default SDKs when nothing says
   otherwise: ESP-IDF, STM32 CubeMX/HAL, nRF Connect SDK (Zephyr), MCUXpresso SDK,
   FX2 with SDCC or Keil C51, FX3 with the FX3 SDK (ThreadX).
2. **Existing structure**: folders, naming, error type, how drivers reach the HAL, where
   generated code lives (CubeMX `Core/`, MCUXpresso `board/`, ESP-IDF components,
   Zephyr devicetree). Follow it unless it is the problem being solved.
3. **Constraints**: RAM and flash budget, real-time deadlines, interrupt priorities,
   power modes, who owns each peripheral, DMA and cache rules, and what must survive
   reset.
4. **What is unclear**: list every open point (pin, bus speed, device address, timing,
   failure behavior, thread or interrupt context of a callback) and ask the developer
   one by one. Do not invent datasheet values, register addresses, or API names; say
   which ones to confirm against the datasheet or SDK version.

## Step 2: Pick the mode

- **Design**: a new module, driver, or product structure. Produce the layer map, the
  objects (struct fields and ops table for each), the ownership of each peripheral and
  buffer, the interrupt-to-task flow, and the open points. Start from
  [references/architecture-layers.md](references/architecture-layers.md); draw a Mermaid
  layer or sequence diagram when more than three modules interact.
- **Implement**: write the code. Follow
  [references/oop-in-c.md](references/oop-in-c.md) for the object shape and
  [references/coding-style.md](references/coding-style.md) for files and names; start
  from [templates/](templates/) when a driver, service, or mock is needed.
- **Port**: move code to another MCU or SDK. Only the port layer (and board files)
  changes; read [references/hal-osal-ports.md](references/hal-osal-ports.md) and the
  platform file. If vendor calls appear in upper layers, that is the first thing to fix.
- **Review**: go through [references/review-checklist.md](references/review-checklist.md)
  and report each finding as `file:line`, the rule it breaks, and the fix.

## Core rules

1. **Dependencies point down; events go up.** `app -> service -> driver -> hal -> port ->
   vendor SDK`. A lower layer never includes a higher one's header; it reports upward
   through a callback or an event queue. Vendor headers (`stm32f4xx_hal.h`,
   `driver/i2c_master.h`, `fsl_*.h`, `<zephyr/...>`, `cyu3*.h`) appear only in port and
   board files. This one rule is what makes porting and host tests possible.
2. **An object is a struct plus functions that take it first.** `uart_t *self` is the
   first parameter of every method; the module name is the prefix (`tmp102_read`).
   State lives in the struct, never in file-scope variables that a second instance would
   share.
3. **An interface is a const table of function pointers.** `struct bus_ops { write, read,
   ... }` in ROM, and a base struct `{ const bus_ops_t *ops; }` embedded as the first
   member of each implementation. Use it where something really varies or must be
   faked in tests (buses, GPIO, clocks, storage, transports), not on every function.
4. **Inject what a module needs.** A driver receives its bus, its reset pin, and its
   callbacks through an init config; it does not reach for a global handle such as
   `hi2c1`. The caller (usually app or board code) wires concrete objects together in
   one place (the composition root).
5. **No runtime heap, bounded everything.** Instances are static or caller-provided
   (`foo_init(&foo_storage, &cfg)`), buffers have a compile-time size, loops and waits
   have timeouts, queues have a depth, stacks have a measured size. `malloc` after
   start-up is a defect because fragmentation shows up in the field, not on the bench.
6. **Interrupt handlers only capture and signal.** Read the status, move or acknowledge
   the data, post to a queue or event, return. Parsing, logging, locking, and heavy
   math run in a task or the main loop. State shared with an ISR is `volatile` or an
   atomic/queue/ring buffer, never a plain variable. See
   [references/interrupts-concurrency.md](references/interrupts-concurrency.md).
7. **Every fallible call returns a status, and every status is checked.** One status
   type per layer boundary (default `fw_status_t`), mapped from the vendor's codes in
   the port. Say per operation whether failure is retried, reported, or fatal.
   ([references/robustness.md](references/robustness.md))
8. **Say what each byte is.** `<stdint.h>` types, explicit endianness and packing for
   wire and register formats (serialize with helpers, do not overlay a struct on a
   buffer), `const` for data that belongs in flash, `static` for everything not
   exported.
9. **Use the SDK's own model instead of fighting it.** Zephyr's `struct device` and
   devicetree, ESP-IDF components and `esp_err_t`, CubeMX `USER CODE` regions, the
   MCUXpresso config tools, the FX3 thread-and-callback model each have a place the
   layers plug into; put thin ports there rather than rebuilding the SDK.
10. **Keep today's design as small as the known needs.** A function table with one
    implementation and no test double is overhead (code size, an indirect call, a harder
    call graph on 8051); add the seam when a second implementation or a mock appears.

## Topic references

| Topic | Reference |
|-------|-----------|
| Layers, dependency rules, folder layout, composition root, layer diagram | [references/architecture-layers.md](references/architecture-layers.md) |
| Objects in C: struct + ops tables, interfaces, "inheritance" with container_of, callbacks with context, state machines, command tables, when not to | [references/oop-in-c.md](references/oop-in-c.md) |
| HAL interfaces (GPIO, I2C, SPI, UART, timer, flash), port layer, OSAL (mutex, queue, delay), bus-device split | [references/hal-osal-ports.md](references/hal-osal-ports.md) |
| ISR rules, ISR-to-task hand-off, ring buffers, critical sections, DMA and cache, priorities | [references/interrupts-concurrency.md](references/interrupts-concurrency.md) |
| Status codes, timeouts, asserts and fault policy, static memory, wire formats, endianness, watchdog | [references/robustness.md](references/robustness.md) |
| Naming, file templates, header rules, comments, configuration macros | [references/coding-style.md](references/coding-style.md) |
| Host-side unit tests: mocks from ops tables, fake clock, how to build and run them | [references/testing-host.md](references/testing-host.md) |
| Generic USB device firmware: descriptors, endpoints, vendor requests, stream and DMA flow | [references/usb-device.md](references/usb-device.md) |
| ESP32: ESP-IDF, FreeRTOS, components, `esp_err_t`, ISR rules | [references/platform-esp32.md](references/platform-esp32.md) |
| STM32: CubeMX regeneration, HAL handles and callbacks, LL, DMA and cache | [references/platform-stm32.md](references/platform-stm32.md) |
| Nordic: nRF Connect SDK, Zephyr devicetree, Kconfig, device API, work queues, Bluetooth | [references/platform-nordic.md](references/platform-nordic.md) |
| NXP: MCUXpresso SDK drivers, config tools, `status_t`, i.MX RT memory and cache | [references/platform-nxp.md](references/platform-nxp.md) |
| FX2LP (8051): memory spaces, SYNCDELAY, EP0 and vendor requests, GPIF, what OOD costs | [references/platform-fx2.md](references/platform-fx2.md) |
| FX3 (ThreadX): threads, callbacks, DMA channels, GPIF II, USB setup | [references/platform-fx3.md](references/platform-fx3.md) |
| Review checklist | [references/review-checklist.md](references/review-checklist.md) |

Read the platform file for the chip in front of you and the topic files the task needs;
do not load them all.

## Templates

[templates/](templates/) holds a small, compiling example of the pattern: shared types
(`fw_common.h`), an I2C bus interface, a sensor interface, a TMP102 driver, a thermal
monitor service, a mock bus, a test harness, and a script that builds and runs the tests
on a PC with gcc, clang, or MSVC. Copy and rename them for a new module instead of
writing the boilerplate from memory, and adapt names and error codes to the project.
[templates/README.md](templates/README.md) says what each file shows.

## Boundaries

- Linux kernel drivers, desktop applications (C#, C++), FPGA/HDL, board design, and
  secure boot or cryptographic internals are out of scope; mention the dependency and
  leave the decision to the developer.
- General object-oriented design (responsibilities, SOLID, patterns) is in
  `ss-design:ood` when it is installed; this skill applies the same ideas under C's
  and the MCU's limits, and takes precedence for firmware.
- Writing to flash, option bytes, fuses, or running a flashing tool changes the device;
  show the command and let the developer run it.
