---
type: llm
focus: trace
weight: 1
---

ESP-IDF I2C calls (for example i2c_master_* functions) appear only in a port file, and that port converts esp_err_t into the project's own status type
