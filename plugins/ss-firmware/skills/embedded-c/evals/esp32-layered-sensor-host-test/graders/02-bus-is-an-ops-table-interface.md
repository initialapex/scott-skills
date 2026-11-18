---
type: llm
focus: trace
weight: 1
---

The driver reaches the I2C bus through an interface defined as a struct holding a pointer to a const table of function pointers, and the bus object is passed in through the driver's init or config, not taken from a global handle
