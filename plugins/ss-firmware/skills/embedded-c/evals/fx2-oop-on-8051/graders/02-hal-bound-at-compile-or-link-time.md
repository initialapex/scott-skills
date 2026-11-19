---
type: llm
focus: trace
weight: 1
---

For the I2C HAL the answer recommends direct calls bound at compile or link time (a header with one implementation per build, or macros/inline) rather than a run-time ops table
