# Firmware review checklist

Use it for a review request, a self-review before a merge, or a port. Report each finding
as `file:line`, the rule it breaks, why it matters on a device, and the fix. Skip
sections that do not apply to the change, and say which you skipped. Rank findings:
**blocker** (corrupts data, hangs, or damages hardware), **major** (breaks layering,
testability, or a budget), **minor** (style, naming).

## 1. Layering and dependencies

- [ ] No vendor header (`stm32*`, `esp_*`, `driver/`, `fsl_*`, `<zephyr/...>`, `nrfx_*`,
      `cyu3*`, RTOS headers) outside port, board, and generated or glue code.
- [ ] Includes point down only; no lower layer includes a higher layer's header; no cycles.
- [ ] Each file's layer is visible from its name and matches what it does (a `drv_` file
      has no vendor calls; a `port_` file has no device knowledge or product logic).
- [ ] Objects are created and wired in one place (composition root); no module picks its own
      bus or reaches for a global handle (`hi2c1`, `huart2`).
- [ ] Upward communication uses callbacks or events, not includes of higher layers.

## 2. Object structure (struct + function pointers)

- [ ] State is in the struct, not in file-scope variables (unless the module is documented as
      single-instance, or the target is the 8051).
- [ ] Methods take `self` first; the module prefix matches the type; private helpers are `static`.
- [ ] `init` validates, copies config values (not a pointer to a stack config), and does not allocate.
- [ ] Ops tables are `static const`; every call goes through a checking wrapper; optional methods
      are documented and `NULL`-checked.
- [ ] The base struct is the first member of a concrete object, and `FW_CONTAINER_OF` (not a
      raw cast) recovers it.
- [ ] Interfaces are small and owned by the consumer; an interface with one implementation has a
      reason (a test double, a second target, or a second implementation planned) or it is removed.
- [ ] Callbacks carry `void *ctx` and state their calling context (ISR, task, any).
- [ ] State machines have one transition site, explicit states, and a safe `default`.

## 3. Interrupts and concurrency

- [ ] ISRs are short: no blocking calls, no logging, no `malloc`, no flash access, no mutex.
- [ ] Only `FromISR`/`_from_isr` RTOS calls in ISRs, at an allowed interrupt priority.
- [ ] Every variable shared with an ISR or another task has one writer and is `volatile`,
      atomic, or behind a queue/ring buffer/critical section; no unprotected read-modify-write.
- [ ] Multi-byte values shared with an ISR are read atomically (critical section or retry); 16/32-bit
      accesses on an 8-bit core are protected.
- [ ] Lost wake-ups handled (flag check + sleep is atomic); events are counted or latched, not
      dropped silently; overflow has a counter.
- [ ] Lock order is documented where two locks exist; no callback is invoked while holding the
      module's lock; mutexes (with inheritance), not binary semaphores, protect shared resources.
- [ ] Task priorities, stack sizes, and queue depths are named constants with a reason; stack
      high-water marks were measured.
- [ ] DMA buffers: reachable memory, aligned and sized for the cache line, cache maintenance
      done, lifetime until completion documented, never on the stack.

## 4. Errors, timeouts, and faults

- [ ] Every fallible call is checked; deliberate ignores are `(void)` with a reason.
- [ ] One status type across layer boundaries; vendor codes are mapped in the port.
- [ ] Output parameters are untouched on failure; no error hidden in a valid-looking value.
- [ ] Every wait has a bound (`timeout_ms`, named constant with its source); timeouts are tested.
- [ ] Time comparisons use subtraction (`(int32_t)(now - deadline) >= 0`), safe across wrap-around.
- [ ] Retry loops are bounded with a back-off; failures are counted and surfaced upward.
- [ ] Asserts only for programmer errors, no side effects in them, policy defined (debug/release).
- [ ] Watchdog fed by a supervisor that proves liveness, not by a timer ISR; reset cause recorded;
      fault handler captures state; outputs go to a safe state.

## 5. Memory and types

- [ ] No `malloc`/`free` after start-up; objects and buffers are static or caller-provided; pools
      for variable-lifetime data have a counter of the lowest free count.
- [ ] No recursion, no variable-length arrays; large buffers are not stack locals.
- [ ] `const` tables are in flash (code-space qualifier on Harvard cores); `static` on everything not
      exported; no variables defined in headers.
- [ ] Fixed-width types for registers, wire data, and storage; `size_t` for sizes; no reliance on
      the width of `int`.
- [ ] Integer promotion and overflow considered (`(uint16_t)b[0] << 8`, signed overflow, sign
      extension of narrow fields, division by zero).
- [ ] No struct overlay on buffers, no bit-fields for hardware or wire formats; explicit
      serialization with endianness helpers; lengths checked before reading.
- [ ] Flash/RAM budget checked from the map file; stack depth known (8051: internal stack and
      call depth).

## 6. Platform specifics (read the matching platform file)

- [ ] ESP-IDF: `ESP_ERROR_CHECK` only in start-up; IRAM-safe ISRs touch only IRAM/DRAM (including
      `const` tables); stack sizes in bytes; DMA memory capabilities; `sdkconfig.defaults` in sync.
- [ ] STM32: only `USER CODE` regions edited in generated files; I2C address shifted exactly once;
      weak callbacks overridden in port files; HAL time base vs RTOS; NVIC grouping and FromISR
      priorities; cache maintenance on F7/H7.
- [ ] Nordic/Zephyr: pins and addresses from devicetree; negative errno handled; no blocking in ISR
      or `k_timer`; work handlers short; Kconfig dependencies verified in the build log.
- [ ] NXP: generated `pin_mux`/`clock_config`/`peripherals` untouched; address convention of the
      driver; blocking transfers bounded; i.MX RT placement of DMA buffers and hot code.
- [ ] FX2LP: `SYNCDELAY` present; both interrupt flags cleared; `HSNAK` or stall on every
      control transfer; memory space on every pointer; reentrancy of shared functions;
      stack budget; descriptors and endpoint config from one set of constants.
- [ ] FX3: DMA buffers 32-byte aligned and sized; streams started and stopped from the app
      thread; teardown on reset/disconnect; endpoint config matches the descriptors per speed;
      `CyU3PReturnStatus_t` mapped, not swallowed.

## 7. Tests and style

- [ ] Drivers and services have host tests against doubles: argument checks, normal path, each
      error path, boundaries, state transitions; tests run in CI with warnings as errors.
- [ ] Parsers and converters have table-driven tests from datasheet or captured data.
- [ ] Hardware constants cite their source (datasheet section, errata, SDK file); units are in names.
- [ ] Comments are in English and explain why; no commented-out code; `TODO(owner)` has an owner.
- [ ] Names follow the module prefix and layer conventions; deviations from MISRA-style rules are
      recorded with a reason.
- [ ] Generated files are regenerated from their source (`.ioc`, `.mex`, GPIF Designer project,
      devicetree), not hand-edited, and the source is committed.

## Report format

```
Summary: <one line: what was reviewed, how many findings by rank>

[blocker] drivers/drv_foo.c:88  Missing timeout on bus wait
  Rule: every wait is bounded (section 4). A held SDA line hangs the sensor task forever
  and the watchdog is fed by the timer ISR, so the system never resets.
  Fix: use hal_i2c_write_read(..., cfg->timeout_ms); map FW_ERR_TIMEOUT to a retry then a fault.

[major] ...
[minor] ...

Not reviewed: <sections skipped and why>
Open questions: <items that need the datasheet or the developer>
```
