---
type: llm
focus: trace
weight: 1
---

The command handling is a separate module that includes no HAL header and dispatches through a const table of command names and function pointers (or equivalent), so it can be tested on a PC
