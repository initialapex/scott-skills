# Objects in C: struct + function pointers

Contents: object = struct + functions · init, deinit, and who owns the memory ·
interface = const ops table · inheritance with a first member · two ways to bind an
implementation · optional methods and default behavior · callbacks with context ·
state machines · command and dispatch tables · strategy and observer · binding time ·
costs · anti-patterns

C has no classes, but the three things that matter (state with its operations,
interchangeable implementations, and a fixed way to call them) are a struct, a function
set, and a const table of function pointers. Use them where something varies or must be
faked; elsewhere a plain function is simpler.

## Object = struct + functions that take it first

```c
/* ring_buffer.h */
typedef struct ring_buffer {
    uint8_t *storage;          /* caller-provided memory */
    size_t capacity;
    volatile size_t head;      /* written by the producer only */
    volatile size_t tail;      /* written by the consumer only */
} ring_buffer_t;

fw_status_t ring_buffer_init(ring_buffer_t *self, uint8_t *storage, size_t capacity);
size_t      ring_buffer_write(ring_buffer_t *self, const uint8_t *data, size_t len);
size_t      ring_buffer_read(ring_buffer_t *self, uint8_t *out, size_t max_len);
size_t      ring_buffer_count(const ring_buffer_t *self);
```

Rules that make it behave like a class:

- **Prefix = type name**, first parameter `self` (`const` when the method does not change
  it). Everything exported is `type_verb`; everything else is `static` in the `.c`.
- **State only in the struct.** A file-scope variable inside a module is a singleton in
  disguise: the second UART or sensor shares it. If a module is truly one-per-device
  (the clock tree, a fault log), say so in the header.
- **Public fields vs private fields.** Visible struct definitions are what allow static
  allocation, so the usual choice is a visible struct with a comment marking what callers
  must not touch (`/* private: */`). Make the type **opaque** (header declares
  `typedef struct foo foo_t;` only) when callers should never allocate it, which requires
  the `.c` to own a static pool (`foo_t s_pool[FOO_MAX]`) and an acquire/release pair.
- **Invariants.** `init` leaves the object valid; every method either checks `self` for
  `NULL` at the public boundary or documents that it does not. Do the check once at the
  entry of the exported function, not in every static helper.

## Init, deinit, and who owns the memory

- **`foo_init(foo_t *self, const foo_config_t *cfg)`** takes caller-provided storage and a
  config struct (see below); it validates, stores, and returns `fw_status_t`. It does
  not allocate. If it must touch hardware, split it: `foo_init` (pure, cannot fail on
  hardware) and `foo_start` / `foo_open` (talks to the device) so tests and the
  composition root stay simple.
- **Config struct, not a long parameter list.** Adding a field later does not break every
  caller, and designated initializers read as documentation:
  `tmp102_config_t cfg = { .bus = bus, .address = 0x48u, .timeout_ms = 0u };`
  (designated initializers need C99; FX2's compilers do not have them, assign fields.)
  A zero in a config field should mean "use the default".
- **`foo_deinit(self)`** only exists if there is something to release (a task, a timer, a
  pin, an interrupt); it undoes `init` in reverse order and leaves the object safe to
  `init` again. Many firmware objects live forever and need none.
- **Copy configs, do not keep a pointer to a stack config.** `self->cfg = *cfg;` is
  correct; `self->cfg = cfg;` where `cfg` was a local is a dangling pointer. Pointers to
  *injected collaborators* (bus, sensor, queue) are meant to be kept: they must outlive
  the object, usually static.
- **Buffers belong to the caller.** Pass `storage, capacity` (as above) or use a macro that
  defines both:

```c
#define RING_BUFFER_DEFINE(name, size)                      \
    static uint8_t name##_storage[(size)];                  \
    static ring_buffer_t name = { name##_storage, (size), 0u, 0u }
```

## Interface = const ops table

```c
/* hal_gpio.h */
typedef struct hal_gpio hal_gpio_t;

typedef struct hal_gpio_ops {
    fw_status_t (*write)(hal_gpio_t *self, bool level);
    fw_status_t (*read)(hal_gpio_t *self, bool *out_level);
    fw_status_t (*set_irq_callback)(hal_gpio_t *self, hal_gpio_cb_t cb, void *ctx); /* optional */
} hal_gpio_ops_t;

struct hal_gpio {
    const hal_gpio_ops_t *ops;   /* const table in flash; the object holds only a pointer */
};

static inline fw_status_t hal_gpio_write(hal_gpio_t *pin, bool level)
{
    if (pin == NULL || pin->ops == NULL || pin->ops->write == NULL) { return FW_ERR_INVALID_ARG; }
    return pin->ops->write(pin, level);
}
```

- **Keep the table `const`.** It lands in flash (read-only data), cannot be overwritten by a
  stray pointer, and costs no RAM. A non-const function pointer table in RAM is a
  corruption target and an easy way to make a stack smash exploitable.
- **One wrapper per method** (`hal_gpio_write`) is the public API; callers do not write
  `pin->ops->write(pin, ...)`. The wrapper is the single place that checks arguments and
  can later add logging or locking.
- **Small interfaces.** Three to six methods that a consumer uses together. A 20-method
  table means two interfaces; consumers that need only `read` should not have to mock `write`.
- **Name methods for the caller's intent** (`read_milli_c`), not for the chip's register
  (`read_reg16`). The interface belongs to the consumer.

## Inheritance: embed the base as the first member

A concrete object *is a* base by containing it, and finds its way back with
`FW_CONTAINER_OF`:

```c
typedef struct port_stm32_i2c {
    hal_i2c_t base;              /* MUST be first: the object is usable as a hal_i2c_t* */
    I2C_HandleTypeDef *handle;   /* vendor type: allowed here, this is a port file */
    osal_mutex_t lock;
} port_stm32_i2c_t;

static fw_status_t i2c_write(hal_i2c_t *base, uint8_t addr7, const uint8_t *data,
                             size_t len, uint32_t timeout_ms)
{
    port_stm32_i2c_t *self = FW_CONTAINER_OF(base, port_stm32_i2c_t, base);
    /* ... */
}

static const hal_i2c_ops_t s_ops = { i2c_write, i2c_write_read };

hal_i2c_t *port_stm32_i2c_as_hal(port_stm32_i2c_t *self) { return &self->base; }
```

- **Upcast** is `&obj->base` (a function `foo_as_bar()` hides it); **downcast** inside a
  method is `FW_CONTAINER_OF`. Use `container_of` even when `base` is the first member: it
  stays correct if someone later reorders the struct, and it documents intent.
- **Single inheritance only.** Need two interfaces? Embed two bases (`base_a`, `base_b`);
  each upcast returns the matching member and `container_of` picks the right one.
- **Extend behavior by composition first.** A `logged_sensor` that wraps a `sensor_t*` and
  implements `sensor_t` (a decorator) is simpler than a deeper hierarchy.
- Do not cast between unrelated struct pointers to "reuse" fields; the compiler is allowed
  to assume they do not alias.

## Two ways to bind an implementation

| Style | Shape | Use when |
|-------|-------|----------|
| **Embedded base** (above) | `{ const ops *ops; }` is the first member of the concrete struct; methods recover it with `container_of` | you own the concrete type (drivers, ports, fakes); the common case |
| **Ops + context pointer** | `{ const ops *ops; void *ctx; }` or a struct of function pointers each taking `void *ctx` | the implementation is something you do not own or cannot embed in: a vendor handle, a Zephyr `struct device`, a closure over one pin |

The second style needs no `container_of` and is ideal for tiny seams:

```c
typedef struct pin_out {
    void (*write)(void *ctx, bool level);
    void *ctx;
} pin_out_t;

/* A driver takes a reset line without knowing what drives it. */
typedef struct { pin_out_t reset; } flash_config_t;
```

## Optional methods and default behavior

- An **optional method** is a `NULL` entry; the wrapper checks it and returns
  `FW_ERR_NOT_SUPPORTED`. Document which entries may be `NULL`.
- A **null object** is a real implementation that does nothing safely (a logger that drops
  messages, a watchdog that never fires). Pass it instead of `NULL` so callers need no
  `if (logger)` everywhere.
- A **default ops table** shared by implementations avoids copy-paste; a derived table is
  a copy with a few entries replaced (`static const ops_t derived = { .read = my_read,
  .write = base_write };`).

## Callbacks with a context pointer

```c
typedef void (*button_cb_t)(void *ctx, uint8_t button_id, button_event_t event);

typedef struct button_service_config {
    button_cb_t on_event;
    void *ctx;                   /* returned unchanged in every call */
} button_service_config_t;
```

- **Always pass `void *ctx`.** A callback without it forces a global to find the object,
  which is how singletons creep in. With it, `ctx` is the object: the callback casts it
  back (`button_ui_t *self = (button_ui_t *)ctx;`).
- **Document the calling context** in the header: "called from the task that calls
  `poll()`", "called from the UART ISR; must not block". Callers cannot guess, and it is
  the most common source of deadlocks and priority-inversion bugs.
- **No re-entrancy surprises.** A callback may call back into the module (to stop it, to
  send a reply). Either state that this is allowed and make the module safe for it
  (update state before calling out), or state that it is not.
- **Vendor callbacks have fixed signatures** (HAL weak callbacks take only a handle).
  Adapt in the port: look the object up from the handle (a small table of
  `{handle, port_object}` or `handle->user_data` where the SDK offers it) and forward to
  your `cb(ctx, ...)`. Upper layers never see the vendor signature.

## State machines

For up to about five states, an `enum` and a `switch` in one function is the clearest
form (see `templates/services/svc_thermal_monitor.c`). Rules: one function owns the
transitions; `state` changes in one place; entry actions run where the state changes; the
`default:` branch recovers to a known state.

For many states or events, use a **transition table** in flash:

```c
typedef enum { EV_START, EV_DONE, EV_ERROR, EV_COUNT } event_t;
typedef enum { ST_IDLE, ST_BUSY, ST_FAILED, ST_COUNT } state_t;

typedef struct machine machine_t;
typedef state_t (*handler_t)(machine_t *self, event_t ev);

static state_t on_start(machine_t *self, event_t ev);
static state_t on_done(machine_t *self, event_t ev);
static state_t on_error(machine_t *self, event_t ev);
static state_t ignore(machine_t *self, event_t ev) { FW_UNUSED(self); FW_UNUSED(ev); return ST_COUNT; }

/* Rows are states, columns are events; ST_COUNT as a result means "stay". */
static const handler_t s_table[ST_COUNT][EV_COUNT] = {
    /* ST_IDLE   */ { on_start, ignore,  ignore   },
    /* ST_BUSY   */ { ignore,   on_done, on_error },
    /* ST_FAILED */ { on_start, ignore,  ignore   },
};

void machine_dispatch(machine_t *self, event_t ev)
{
    state_t next;

    if ((unsigned)self->state >= ST_COUNT || (unsigned)ev >= EV_COUNT) { return; }
    next = s_table[self->state][ev](self, ev);
    if (next != ST_COUNT) { self->state = next; }
}
```

Feed events from one place (a queue read by one task), never from several contexts at once.
Keep handlers free of blocking calls; start the slow thing and let its completion event
come back in.

## Command and dispatch tables

A sorted or small unsorted `const` array of `{id, handler}` replaces a long `switch` and
is the natural shape for a serial CLI, a binary protocol, USB vendor requests, and
register maps:

```c
typedef struct command {
    uint8_t opcode;
    fw_status_t (*handler)(void *ctx, const uint8_t *args, size_t len, uint8_t *resp, size_t *resp_len);
    uint8_t min_args;
} command_t;

static const command_t s_commands[] = {
    { CMD_PING,      cmd_ping,      0u },
    { CMD_SET_LEVEL, cmd_set_level, 2u },
    { CMD_GET_INFO,  cmd_get_info,  0u },
};

fw_status_t command_dispatch(void *ctx, uint8_t opcode, const uint8_t *args, size_t len,
                             uint8_t *resp, size_t *resp_len)
{
    size_t i;

    for (i = 0u; i < FW_ARRAY_SIZE(s_commands); i++) {
        if (s_commands[i].opcode == opcode) {
            if (len < s_commands[i].min_args) { return FW_ERR_INVALID_ARG; }
            return s_commands[i].handler(ctx, args, len, resp, resp_len);
        }
    }
    return FW_ERR_NOT_SUPPORTED;
}
```

The table is data: adding a command is one row and one function, and a host test can
iterate the table to check for duplicate opcodes. It is the one place the pattern pays off
even on an 8051 (`platform-fx2.md`).

## Strategy and observer

- **Strategy**: an ops table selected at init or at runtime for an algorithm that varies
  (a filter, a retry policy, a checksum). Same shape as an interface; swapping means
  assigning another `const ops *`.
- **Observer**: a fixed array of `{cb, ctx}` slots (`SUBSCRIBERS_MAX`) with `subscribe` and
  `notify`; fixed capacity returns `FW_ERR_NO_MEM` when full. Notify from one context,
  copy the slot before calling (a callback may unsubscribe), and do not notify from an ISR
  unless every subscriber is ISR-safe: post an event instead.

## Binding time: run-time table or link-time port

| Binding | How | Cost | Pick when |
|---------|-----|------|-----------|
| **Link-time** | one header, a different `.c` per platform; the build selects which `.c` is compiled | zero: direct calls, inlining, no table | exactly one implementation per build and tests can link a fake `.c` instead |
| **Run-time ops table** | const table + pointer in the object | an indirect call, a pointer per object, no inlining | several implementations in one build (two buses of different kinds), or tests that need per-test scripting |
| **Compile-time macro / inline** | `#define` or `static inline` in a header | zero | the operation is one or two instructions in a hot path (GPIO toggle in an ISR) |

Link-time binding also makes host tests easy (link `fake_gpio.c` instead of
`port_stm32_gpio.c`), and is the right default on 8051 and for very small MCUs. Use the
ops table when one binary holds more than one implementation or when a mock needs
different behavior in each test.

## Costs and limits

- An indirect call is a few cycles and defeats inlining and some static analysis (stack
  depth, call graphs, dead-code removal): do not put it in the innermost loop of an ISR or
  DSP kernel.
- Each object carries one pointer; each table costs one flash word per method.
- Function pointers are what MISRA-C Rule 11.x and CFI-minded reviewers watch: keep tables
  `const`, check for `NULL` in the wrapper, and never cast a function pointer to another
  signature or to `void *`.
- Recursion through callbacks (callback -> method -> callback) can exhaust a small stack;
  bound it by design.
- Keil C51 and SDCC on 8051 treat function pointers specially (reentrancy, overlay,
  generic pointers); read [platform-fx2.md](platform-fx2.md) before using tables there.

## Anti-patterns

- A "manager" with a `static` instance and public globals that all modules poke (a
  singleton with no seam). Pass the object.
- `void *` parameters standing in for a type. Use a typed pointer or a typed callback
  with `void *ctx` only for the opaque user context.
- Callbacks without a context pointer, or with an undocumented calling context.
- A mutable ops table, or an ops table copied into every object (RAM waste).
- One giant `config` struct shared by unrelated modules: each module owns its own config.
- An interface created "in case" with one implementation, no test, and no second target.
- Hiding blocking behavior behind a method that looks cheap (`sensor_read` that sleeps
  100 ms): the ops contract must state blocking and time limits (`timeout_ms` parameter).
