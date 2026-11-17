# scott-skills

Scott Chang 的 Claude Code plugin marketplace。

[English](README.md)

## Plugins

| Plugin | 來源 | 說明 |
|--------|------|------|
| `ss-workflow` | [initialapex/ss-workflow](https://github.com/initialapex/ss-workflow)（獨立 repository） | 以 request 驅動的 gitflow 工作流：init、new/check request、review、merge、release |
| `ss-wpf` | [plugins/ss-wpf](plugins/ss-wpf) | WPF 桌面應用程式。`ss-wpf:mvvm`：MVVM，.NET 8 以上和 .NET Framework 4.7.2/4.8 用 CommunityToolkit.Mvvm，Visual Studio 2012 用手寫的基底類別 |

規劃中（這個 marketplace 的 plugin 一律加上 `ss-` 前綴，讓 skill 的完整名稱 `<plugin>:<skill>` 不會跟其他 marketplace 的 plugin 重複）：

| Plugin | Skills | 範圍 |
|--------|--------|------|
| `ss-design` | `ood`、`ui-ux` | 與技術無關的設計原則與 review checklist |
| `ss-wpf` | `material-design`（加進既有的 plugin） | WPF：MaterialDesignInXaml |

## 安裝

在 Claude Code session 裡執行：

```
/plugin marketplace add initialapex/scott-skills
/plugin install ss-workflow@scott-skills
/plugin install ss-wpf@scott-skills
```

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
