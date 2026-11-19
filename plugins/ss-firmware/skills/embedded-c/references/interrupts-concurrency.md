# Interrupts, tasks, and shared data

Contents: ISR rules · getting from an ISR to a task · a lock-free ring buffer · shared
variables and atomicity · critical sections · priorities · tasks and locks · DMA and
cache · sleeping safely · testing ISR code

Concurrency bugs pass the bench and fail in the field, because they depend on timing.
The defense is a design where every piece of shared data has one writer and a named hand-off
mechanism, not testing for the race afterwards.

## ISR rules

1. **Short and bounded.** Read the status, move the data or arm the next transfer,
   acknowledge the source, signal a task, return. Microseconds, not milliseconds.
2. **No blocking calls**: no mutex, no `delay`, no queue send with a timeout, no `malloc`,
   no waiting on a flag. Use the RTOS's `_FromISR` / `_from_isr` variants.
3. **No logging, `printf`, or file/flash access** inside an ISR (they block, take locks,
   or use large stacks). Set a flag or count an event and report from a task.
4. **Know what it may call.** On an RTOS, only calls documented as ISR-safe, and only from
   an interrupt whose priority allows RTOS calls (see Priorities).
5. **Everything an ISR and another context share is `volatile` or goes through a
   queue/ring buffer/atomic**, and has one writer.
6. **The ISR is a thin shell around a normal function.** The vector (in the port) calls
   `uart_driver_on_irq(&s_uart)`; the function takes `self`, so a host test can call it
   with fake register values. No logic in the vector itself.
7. **Mark ISR-callable functions and ISR-called callbacks** in the header ("ISR context:
   must not block"). Vendor annotations matter on some chips (`IRAM_ATTR` on ESP32 so the
   handler is not in flash while the cache is off).
8. **Interrupt stack depth counts.** On Cortex-M the main stack (MSP) is used by ISRs;
   size it for the deepest nesting; an exception frame that carries FPU state is 104
   bytes instead of 32 (Cortex-M4F/M7).

## From ISR to task

| Mechanism | Use when | Notes |
|-----------|----------|-------|
| **Flag + poll** (bare-metal super-loop) | one event, no RTOS | `volatile bool`; the loop clears it; set and clear by different contexts must not race (disable interrupts around read-modify-clear if a second event can arrive) |
| **Task notification / semaphore give** | "wake the task, it will read the hardware" | cheapest; counts events if a counting semaphore |
| **Queue of small items** | the ISR has data (a byte, an event struct) and order matters | fixed item size; `FromISR` send returns whether a higher-priority task woke, and the ISR must yield on it |
| **SPSC ring buffer + notification** | a stream (UART RX, ADC samples) | the ISR writes bytes, the task reads; notify on "idle line" or a threshold, not per byte |
| **Deferred work item** | the handler needs thread context (Zephyr `k_work_submit`, FreeRTOS `xTimerPendFunctionCallFromISR`, ESP-IDF event loop post) | handler runs later at task priority |

Pattern (FreeRTOS shape; ESP-IDF and CubeMX use the same calls):

```c
void UARTx_IRQHandler(void)                       /* in the port: a thin shell */
{
    BaseType_t woken = pdFALSE;

    port_uart_on_irq(&s_uart0, &woken);           /* the logic; testable on a PC */
    portYIELD_FROM_ISR(woken);
}

/* port_uart.c */
void port_uart_on_irq(port_uart_t *self, BaseType_t *woken)
{
    while (rx_ready(self)) {
        uint8_t b = read_byte(self);
        if (ring_buffer_write(&self->rx, &b, 1u) == 0u) { self->rx_overflows++; }
    }
    if (idle_line(self)) { vTaskNotifyGiveFromISR(self->consumer, woken); }
}
```

Always count drops (`rx_overflows`); a silent loss is a bug you cannot find later.

## A lock-free single-producer single-consumer ring buffer

When exactly one context writes (`head`) and exactly one reads (`tail`), no lock is
needed. On a single-core MCU where the writer is an ISR and the reader a task, `volatile`
indices are enough; with two cores, DMA writing the buffer, or a Cortex-M7 data cache in
between, add the barriers shown.

```c
size_t ring_buffer_count(const ring_buffer_t *self)
{
    size_t head = self->head;
    size_t tail = self->tail;

    return (head >= tail) ? (head - tail) : (self->capacity - tail + head);
}

size_t ring_buffer_write(ring_buffer_t *self, const uint8_t *data, size_t len)   /* producer */
{
    size_t head = self->head;
    size_t space = self->capacity - 1u - ring_buffer_count(self);   /* one slot stays empty */
    size_t n = (len < space) ? len : space;
    size_t i;

    for (i = 0u; i < n; i++) {
        self->storage[head] = data[i];
        head = (head + 1u == self->capacity) ? 0u : head + 1u;
    }
    FW_BARRIER();             /* data visible before the index moves: __DMB() or atomic release */
    self->head = head;
    return n;
}

size_t ring_buffer_read(ring_buffer_t *self, uint8_t *out, size_t max_len)       /* consumer */
{
    size_t tail = self->tail;
    size_t n = ring_buffer_count(self);
    size_t i;

    n = (n < max_len) ? n : max_len;
    FW_BARRIER();             /* index read before the data */
    for (i = 0u; i < n; i++) {
        out[i] = self->storage[tail];
        tail = (tail + 1u == self->capacity) ? 0u : tail + 1u;
    }
    FW_BARRIER();             /* finish reading before the slots are handed back to the producer */
    self->tail = tail;
    return n;
}
```

`FW_BARRIER()` is `__DMB()` on Cortex-M with DMA or two cores, a C11 `atomic_thread_fence`
where available, and nothing on a single-core MCU with no DMA into the buffer. Define it
once in common. Write the tests for full, empty, wrap-around, and partial writes on the
PC.

## Shared variables and atomicity

- `volatile` forces each access to happen; it does **not** make `x++` atomic, and it does
  not order accesses to different variables on a multi-core part.
- On Cortex-M a naturally aligned 8-, 16-, or 32-bit load or store is atomic; 64-bit is
  not. A read-modify-write (`count++`, `flags |= x`) is **not** atomic against an ISR: use
  a critical section, a hardware bit-set register, an LDREX/STREX atomic, or give the
  variable a single writer.
- On an 8051 (FX2), even a 16-bit read is two instructions: disable the interrupt or
  read until two successive reads agree.
- A struct shared with an ISR is copied out inside a critical section (or double
  buffered with an index flip), never read field by field.
- **Prefer hand-off to sharing**: the ISR enqueues a copy, the task owns it afterwards.

## Critical sections

Keep them to a few instructions; they add directly to interrupt latency.

| Environment | Enter / leave |
|-------------|---------------|
| Cortex-M bare-metal | `uint32_t primask = __get_PRIMASK(); __disable_irq(); ... __set_PRIMASK(primask);` (restores the previous state, so it nests) |
| FreeRTOS (task) | `taskENTER_CRITICAL()` / `taskEXIT_CRITICAL()`; in an ISR `taskENTER_CRITICAL_FROM_ISR()` / `taskEXIT_CRITICAL_FROM_ISR(saved)` |
| ESP-IDF (dual core) | `portMUX_TYPE lock = portMUX_INITIALIZER_UNLOCKED; portENTER_CRITICAL(&lock) ... portEXIT_CRITICAL(&lock)` (a spinlock is needed across both cores); `portENTER_CRITICAL_ISR` in an ISR |
| Zephyr | `unsigned int key = irq_lock(); ... irq_unlock(key);`, or a spinlock (`k_spin_lock`) on SMP |
| 8051 (SDCC / Keil) | `EA = 0; ... EA = saved_EA;` (save the old state; do not blindly set `EA = 1`) |

Put these behind one `fw_critical_enter/exit` pair in `common` or `osal`, so modules
stay portable and the PC build maps them to nothing.

## Priorities

- **Cortex-M NVIC**: a numerically *lower* priority value is *more urgent*. The number of
  implemented priority bits is chip-specific (STM32: 4, nRF52: 3, i.MX RT: 4); the CMSIS
  `NVIC_SetPriority()` takes the logical value, not the shifted register value.
- **FreeRTOS on Cortex-M**: an ISR that calls any `...FromISR` function must have a
  priority numerically **greater than or equal to** `configMAX_SYSCALL_INTERRUPT_PRIORITY`
  (logically no more urgent than that limit). Interrupts more urgent than the limit are
  allowed but may not call the RTOS at all. Use all priority bits for preemption priority
  (`NVIC_PRIORITYGROUP_4` on STM32), or the numbers do not mean what the config says.
- **Zephyr**: `IRQ_CONNECT(irq, priority, isr, arg, flags)`; "direct" ISRs
  (`ISR_DIRECT_DECLARE`) bypass the kernel and must not call kernel APIs that need the
  scheduler.
- **Task priorities**: give priority by deadline, not by importance; a task that blocks on
  a queue most of its life can run at high priority; a task that computes runs lower.
  Write the assignment down next to the task creation.

## Tasks and locks

- **One owner per resource.** Prefer a task that owns a peripheral and takes requests
  through a queue over many tasks locking it. The owner is simple to reason about and
  needs no lock.
- **If a lock is needed**: use a mutex with priority inheritance (FreeRTOS
  `xSemaphoreCreateMutex`, Zephyr `k_mutex`), not a binary semaphore, to avoid priority
  inversion; always with a timeout; never held across a call that can block for long;
  never taken in an ISR.
- **Lock order**: when two locks are ever held together, fix the order in a comment and
  keep it everywhere; better, restructure so one is never held while taking the other.
- **Callbacks and locks**: do not call a callback while holding the module's lock; the
  callback may call back in.
- **Stacks**: measure, do not guess. FreeRTOS `uxTaskGetStackHighWaterMark`, Zephyr
  `k_thread_stack_space_get` / thread analyzer, ESP-IDF `uxTaskGetStackHighWaterMark` and
  `CONFIG_FREERTOS_CHECK_STACKOVERFLOW`. Leave 25 percent margin, enable the overflow
  hook in debug builds. Large local arrays (a 512-byte buffer) belong to `static` storage
  owned by the object, not the stack.
- **Timers**: RTOS software-timer callbacks run in a shared timer task; they must not block
  and must be short, or every timer in the system stalls.

## DMA and cache

- **DMA needs memory the DMA engine can reach.** STM32 H7/F7 DTCM and some CCM RAM are not
  DMA-visible; ESP32 DMA buffers come from internal RAM (`DMA_ATTR`, or
  `heap_caps_malloc(..., MALLOC_CAP_DMA)` at start-up); i.MX RT DMA needs OCRAM or a
  non-cacheable region; FX3 DMA buffers come from the SDK's DMA buffer area.
- **Cache coherence** (Cortex-M7 and any part with a D-cache): clean (write back) the
  cache lines before a memory-to-peripheral transfer; invalidate them after a
  peripheral-to-memory transfer; align buffers and sizes to the cache line (32 bytes on
  M7 and FX3), or place them in a non-cacheable section (`AT_NONCACHEABLE_SECTION` in the
  NXP SDK, an MPU region on STM32). A buffer that shares a cache line with other data gets
  that data corrupted by an invalidate.
- **Lifetime**: a buffer given to an asynchronous DMA call stays valid and untouched until
  the completion callback; document it in the interface, and keep it in the object, not on
  the caller's stack.
- **Double buffering**: ping-pong buffers with half- and full-transfer callbacks; the
  callback only flips ownership and signals; processing runs in a task and must finish
  before the other half completes (check and count overruns).

## Sleeping safely

The classic lost wake-up: check a flag, then `__WFI()`, with the interrupt arriving between
the two. Make the check-and-sleep atomic: disable interrupts, check the flag, `__WFI()`
(a pending interrupt still wakes the core even with interrupts masked), then enable
interrupts. On an RTOS, block on the queue or semaphore and let the idle hook or tickless
idle sleep. Do not enter deep sleep with a DMA transfer, a flash write, or a radio event
pending; each platform names the gating call (`pm_policy`, `esp_pm_lock`, SDK power
managers); use it.

## Testing ISR code

- The handler function (`xxx_on_irq(self)`) takes the object and reads hardware through
  the port or injected registers; a host test calls it directly with scripted status and
  checks the ring buffer, the counters, and the signal raised.
- Race conditions are checked by **reasoning on the ownership table** (who writes each
  variable, which context reads it, what hands it over), recorded in the design, and by
  stress tests on target (long runs with maximum interrupt load), not by a passing unit test.
