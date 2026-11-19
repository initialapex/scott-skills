---
type: llm
focus: trace
weight: 1
---

The design has an application thread that waits on event flags (or an equivalent queue) set by the USB event and setup callbacks, and starting or stopping the stream is done in that thread, not inside the callbacks
