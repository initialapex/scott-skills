---
type: llm
focus: trace
weight: 1
---

The HAL weak receive callback is overridden in a port or board file (not in main.c) and forwards the event out of the HAL layer; if more than one UART instance is plausible, it finds its object by a handle registry or context pointer rather than a hard-coded handle
