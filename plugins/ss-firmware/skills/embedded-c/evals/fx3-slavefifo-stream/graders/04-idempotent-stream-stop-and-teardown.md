---
type: llm
focus: trace
weight: 1
---

There is a stream stop operation that is safe to call in any state (aborts or destroys the DMA channel, flushes endpoints, disables the GPIF state machine) and it is called on USB reset, disconnect and when the configuration is cleared
