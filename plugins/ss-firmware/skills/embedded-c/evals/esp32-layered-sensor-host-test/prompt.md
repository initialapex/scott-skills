---
tags: [skill-creator]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

我用 ESP32-S3、ESP-IDF 5.x，要接一顆 SHT31 溫濕度感測器（I2C，位址 0x44），每 2 秒讀一次，溫度超過 60 度要通知 app。我想要分層的架構，之後可以在 PC 上做單元測試，不要綁死 ESP-IDF。幫我規劃檔案結構，並寫出 driver、service 和 ESP32 port 的主要程式碼。
