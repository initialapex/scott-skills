# Coding style for firmware modules

Contents: language level and MISRA stance · naming · file layout and headers · file
skeletons · comments · formatting · logging · deviations

Follow the project's existing style first (formatter config, naming, brace style). This
file is the default for new code and for deciding what a reviewer should flag.

## Language level and MISRA stance

- **C99** (`-std=c99` or `gnu99`): `<stdint.h>`, `<stdbool.h>`, `static inline`, designated
  initializers, declarations after statements, `//` comments are available. The Zephyr
  (C99/C11 + GNU extensions), ESP-IDF (GNU C11/C17), STM32, NXP, and FX3 toolchains all
  accept it. FX2 compilers do not: see [platform-fx2.md](platform-fx2.md).
- **MISRA-C in spirit**, not enforced rule by rule: no recursion, no dynamic memory after
  start-up, no `goto` except one cleanup label, no function-like macros with side effects,
  braces on every `if/else/for/while`, a `default:` in every `switch`, no implicit
  conversions that lose data, every function has one clear return type and checks what
  it calls, no use of uninitialized variables, no dead or unreachable code, no `printf`
  family in production paths without a bound. If the customer requires MISRA compliance,
  run a checker and record deviations (below).
- **Vendor code keeps the vendor's style.** Do not reformat generated or SDK files.

## Naming

| Item | Convention | Example |
|------|-----------|---------|
| Module prefix | the file stem, lower case, one prefix per module | `tmp102_`, `thermal_monitor_`, `ring_buffer_` |
| Layer in the file name | `hal_`, `drv_`, `svc_`, `port_<platform>_`, `board_`, `osal_` | `drv_tmp102.c`, `port_stm32_i2c.c`, `hal_i2c.h` |
| Types | `<module>_t` for objects, `<module>_config_t`, `<module>_ops_t`, `<module>_event_t` | `tmp102_t`, `hal_i2c_ops_t` |
| Functions | `<module>_<verb>[_<object>]`, lower case | `tmp102_read_milli_c`, `ring_buffer_write` |
| Methods | `self` first parameter | `fw_status_t foo_start(foo_t *self)` |
| Callback types | `<module>_cb_t`, with `void *ctx` first | `typedef void (*button_cb_t)(void *ctx, ...)` |
| Enum constants | UPPER_CASE with the type's prefix | `THERMAL_EVENT_OVER_TEMP`, `FW_ERR_IO` |
| Macros and constants | UPPER_CASE with the module prefix | `TMP102_REG_TEMPERATURE`, `UART_RX_BUFFER_SIZE` |
| File-scope variables | `s_` prefix, `static` | `s_tmp102_sensor_ops`, `s_uart0` |
| Globals (avoid) | `g_` prefix, declared in one header | `g_board` |
| Interrupt handlers | the vector name the startup file expects, only in port files; they call `<module>_on_irq(self)` | `USART2_IRQHandler` |
| Units in names | suffix every quantity with its unit | `timeout_ms`, `freq_hz`, `value_milli_c`, `len_bytes` |

Say what a thing is in the domain's words (`thermal_monitor`, `flash_log`), not `manager`,
`helper`, `util`, `common2`, or `data`.

## File layout and headers

- **One module = one `.h` + one `.c`**; the header is the contract (types, functions,
  ownership and calling-context rules), the source is hidden. Private helpers are
  `static` in the `.c`; private types stay in the `.c` unless the caller must allocate them.
- **Include guards** named after the file (`DRV_TMP102_H`), not `#pragma once` alone
  (not every embedded toolchain honors it). Add `extern "C"` guards when C++ code may
  include the header (Arduino-style sketches, GoogleTest-based host tests).
- **A header includes what it needs and nothing more**: `fw_common.h` for types, the
  interface headers it uses; it must compile alone. Forward-declare a struct instead of
  including its header when only a pointer is used.
- **Include order in a `.c`**: its own header first (proves the header is self-contained),
  then project headers by layer, then the C library, then vendor headers (port files
  only).
- **No definitions in headers** except `static inline` wrappers and `static const` tables
  small enough to duplicate. Never define a variable in a header.
- **Public API surface is small.** Export only what other modules call; mark everything
  else `static`. Tables of function pointers are `static const` in the `.c` and reached
  through the object.
- **Header contract comments** state, for each function: what it does, the units, whether
  it blocks (and the time limit), which context may call it (task, ISR, any), what it
  returns on failure, and who owns buffers.

## File skeletons

Header:

```c
/*
 * drv_foo.h - driver for the Foo sensor (driver layer).
 *
 * Context: all functions are called from tasks; none may be called from an ISR.
 * Blocking: every transfer is bounded by cfg.timeout_ms.
 */
#ifndef DRV_FOO_H
#define DRV_FOO_H

#include "fw_common.h"
#include "hal_i2c.h"

#ifdef __cplusplus
extern "C" {
#endif

typedef struct foo_config {
    hal_i2c_t *bus;      /* required */
    uint8_t address;     /* 7-bit address */
    uint32_t timeout_ms; /* 0 selects the default */
} foo_config_t;

typedef struct foo {
    /* private: do not access the fields directly */
    hal_i2c_t *bus;
    uint8_t address;
    uint32_t timeout_ms;
} foo_t;

fw_status_t foo_init(foo_t *self, const foo_config_t *cfg);
fw_status_t foo_read(foo_t *self, int32_t *out_value);

#ifdef __cplusplus
}
#endif

#endif /* DRV_FOO_H */
```

Source:

```c
#include "drv_foo.h"

#define FOO_REG_DATA 0x00u
#define FOO_DEFAULT_TIMEOUT_MS 20u

fw_status_t foo_init(foo_t *self, const foo_config_t *cfg)
{
    if (self == NULL || cfg == NULL || cfg->bus == NULL) {
        return FW_ERR_INVALID_ARG;
    }
    self->bus = cfg->bus;                    /* collaborator: kept, must outlive the object */
    self->address = cfg->address;            /* values: copied */
    self->timeout_ms = (cfg->timeout_ms != 0u) ? cfg->timeout_ms : FOO_DEFAULT_TIMEOUT_MS;
    return FW_OK;
}
```

## Comments

- **Comments are in English**, in every language and file type (C, C++, C#, Python,
  PowerShell, Bash, batch, linker scripts), regardless of the language of the
  conversation. Documentation outside the source (README, design notes) follows the
  project's own language.
- Explain **why** and the constraint (`/* datasheet table 7: 6 ms conversion at 12 bit */`),
  not what the next line does. Name magic numbers with a macro instead of a comment.
- Cite the **source of every hardware constant** (datasheet section, errata number, SDK
  file) next to it. A number without a source cannot be reviewed.
- Record the **units and the valid range** on struct fields and function parameters.
- Mark open work with `TODO(owner):` and a reason; do not leave commented-out code.
- Use `/* ... */` in code that must compile on C89-style toolchains (FX2, older Keil); `//`
  is fine elsewhere. Pick one per file.

## Formatting

- Use the project's `.clang-format` if present; otherwise: 4-space indent (no tabs),
  100 columns, braces on the same line for control statements and on the next line for
  function definitions, one statement per line, one declaration per line, `*` attached
  to the name (`uint8_t *buf`).
- **Always brace** `if/else/for/while` bodies, including one-liners.
- Initialize variables where declared; on toolchains that need C89 declarations (FX2), at
  the top of the block.
- Functions: one job, about 40 lines, at most four or five parameters (a config struct
  beyond that), no more than three levels of nesting. Long `switch` tables belong in a
  data table.
- Use `static inline` functions instead of function-like macros; where a macro is needed,
  parenthesize every argument and evaluate each once.
- Use the unsigned suffix on constants that are not negative (`0x01u`, `1000u`).

## Logging

- Route logging through one macro set (`FW_LOGE`, `FW_LOGW`, `FW_LOGI`, `FW_LOGD`) defined
  per port onto the SDK (`ESP_LOGE`, Zephyr `LOG_ERR`, `printf` over UART/RTT/SWO, FX3
  `CyU3PDebugPrint`), so modules stay portable and the PC build prints to stdout or
  nothing.
- A log line carries the module tag and the values needed to understand it. Never log from
  an ISR; never log in a loop at full rate (rate-limit or count).
- Production builds keep errors and a few state transitions; verbose levels compile out
  (they also cost flash for the strings).

## Deviations

When a rule is broken on purpose (a cast to reach a register, an `#ifdef` that cannot be
avoided, a function pointer cast in the SDK interface), put the reason where it happens:

```c
/* DEVIATION(MISRA 11.4): the register block address is fixed by the datasheet (table 3). */
```

and keep the number of deviations small enough to list in the review.
