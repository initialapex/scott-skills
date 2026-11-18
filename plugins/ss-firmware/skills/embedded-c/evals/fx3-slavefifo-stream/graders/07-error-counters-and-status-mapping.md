---
type: llm
focus: trace
weight: 1
---

The design counts overruns or DMA errors and returns them through a vendor request, and SDK return codes (CyU3PReturnStatus_t) are handled or mapped rather than ending in an infinite-loop error handler
