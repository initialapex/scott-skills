# scott-skills

Scott Chang 的 Claude Code plugin marketplace。

[English](README.md)

## Plugins

| Plugin | 來源 | 說明 |
|--------|------|------|
| `ss-workflow` | [initialapex/ss-workflow](https://github.com/initialapex/ss-workflow)（獨立 repository） | 以 request 驅動的 gitflow 工作流：init、new/check request、review、merge、release |

規劃中：設計類 skills（物件導向設計、MVVM、Material Design、UI/UX 設計）。

## 安裝

這個 repository 是私有的，你的 git 必須能 clone 它（`gh auth login` 或 SSH key）。在 Claude Code session 裡執行：

```
/plugin marketplace add initialapex/scott-skills
/plugin install ss-workflow@scott-skills
```

私有 marketplace 的背景自動更新需要設定 `GITHUB_TOKEN`（或 `GH_TOKEN`）環境變數，且對這個 repository 有讀取權限。

## 結構

```
.claude-plugin/marketplace.json   列出所有 plugin
plugins/<plugin>/                 放在這個 repository 裡的 plugin
```

有自己發版週期的 plugin（例如 `ss-workflow`）留在自己的 repository，這裡用 URL 登錄；放在這裡的 plugin 用相對路徑（`./plugins/<plugin>`）登錄。

## 版本

- 每個 plugin 在自己的 `.claude-plugin/plugin.json` 管理 `version`。
- 這個 repository 裡的 plugin 打 `<plugin>/vX.Y.Z` 格式的 tag。
- `main` 只放已發佈的版本，日常開發在 `develop`。
