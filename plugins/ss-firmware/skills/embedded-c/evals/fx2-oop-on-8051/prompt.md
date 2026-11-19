---
tags: [skill-creator]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

FX2LP（CY7C68013A）用 SDCC + fx2lib 開發。我習慣用 struct + function pointer 做物件，想把 vendor request（讀寫 I2C EEPROM、控制 GPIO、切換 slave FIFO 模式）做成 driver 物件，每個 driver 有自己的 ops 表，另外 hal 層也用 ops 表來抽象 I2C。這樣在 FX2 上可以嗎？請給我建議的寫法和範例。
