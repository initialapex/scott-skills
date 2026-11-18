---
tags: [skill-creator]
max_turns: 30
timeout_seconds: 900
allowed_tools: [Read, Glob, Grep, Skill, Write, Edit, Bash]
---

FX3（CYUSB3014）專案：外部 FPGA 用 GPIF II slave FIFO 把資料灌進來，要透過 USB 3.0 bulk IN 傳給 PC。PC 端用 vendor request 開始／停止串流、查詢錯誤計數。請幫我規劃韌體架構（thread、callback、DMA channel、vendor request 處理），並寫出主要骨架程式碼。
