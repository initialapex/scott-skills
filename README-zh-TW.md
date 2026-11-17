# scott-skills

Scott Chang 的 Claude Code plugin marketplace。

[English](README.md)

## Plugins

| Plugin | 來源 | 說明 |
|--------|------|------|
| `ss-workflow` | [initialapex/ss-workflow](https://github.com/initialapex/ss-workflow)（獨立 repository） | 以 request 驅動的 gitflow 工作流：init、new/check request、review、merge、release |
| `ss-wpf` | [plugins/ss-wpf](plugins/ss-wpf) | WPF 桌面應用程式。`ss-wpf:mvvm`：MVVM，.NET 8 以上和 .NET Framework 4.7.2/4.8 用 CommunityToolkit.Mvvm，Visual Studio 2012 用手寫的基底類別。`ss-wpf:material-design`：MaterialDesignThemes（MaterialDesignInXaml）5.x 的主題、樣式、對話框與 shell，適用 .NET 8 以上和 .NET Framework 4.7.2/4.8 |
| `ss-design` | [plugins/ss-design](plugins/ss-design) | 與技術無關的設計。`ss-design:ood`：物件導向設計（責任分配、SOLID、設計模式、領域模型、code smell 與重構、錯誤處理），附 Mermaid 圖與 review checklist；範例為 C# 7.3。`ss-design:ui-ux`：UI/UX 設計，先決定 Easy to use 或 Easy to learn（資訊架構與流程、版面、表單／表格與回饋、鍵盤與無障礙、多語系、現場操作畫面），附 Mermaid 流程圖、ASCII 線框圖與 review checklist |

這個 marketplace 的 plugin 一律加上 `ss-` 前綴，讓 skill 的完整名稱 `<plugin>:<skill>` 不會跟其他 marketplace 的 plugin 重複。

## 安裝

在 Claude Code session 裡執行：

```
/plugin marketplace add initialapex/scott-skills
/plugin install ss-workflow@scott-skills
/plugin install ss-wpf@scott-skills
/plugin install ss-design@scott-skills
```

### 離線安裝

每個 [GitHub Release](https://github.com/initialapex/scott-skills/releases) 都附有
`scott-skills-vX.Y.Z.zip`，內含整個 marketplace（包括 `ss-workflow`）。解壓後把資料夾加成本機 marketplace：

```
/plugin marketplace add C:\path\to\scott-skills-vX.Y.Z
/plugin install ss-workflow@scott-skills
/plugin install ss-wpf@scott-skills
/plugin install ss-design@scott-skills
```

## 結構

```
.claude-plugin/marketplace.json   列出所有 plugin
plugins/<plugin>/                 放在這個 repository 裡的 plugin
tools/ReleasePack/                發佈打包工具（不屬於任何 plugin）
```

有自己發版週期的 plugin（例如 `ss-workflow`）留在自己的 repository，這裡用 URL 登錄；放在這裡的 plugin 用相對路徑（`./plugins/<plugin>`）登錄。

## 版本

- 整個 collection 的版號放在 `.claude-plugin/marketplace.json` 的 `metadata.version`；
  repository 每次發佈都會更新它，並打 `vX.Y.Z` 格式的 tag。
- 每個 plugin 在自己的 `.claude-plugin/plugin.json` 管理 `version`。
- 這個 repository 裡的 plugin 在發佈時若版號有變，打 `<plugin>/vX.Y.Z` 格式的 tag。
- 發佈走 `release/vX.Y.Z` 分支，以 collection 版號命名。
- `main` 只放已發佈的版本，日常開發在 `develop`。

## 發佈流程

1. 從 `develop` 開 `release/vX.Y.Z`，更新 `marketplace.json` 的 `metadata.version`（以及有改動的
   plugin 的 `version`），合進 `main` 並打 tag，再合回 `develop`，然後推送 `main`、`develop` 和 tag。
2. 從 tag 打包（需要 .NET 10 SDK、git、gh；有安裝 claude CLI 時會一併驗證）：

   ```powershell
   dotnet run --project tools/ReleasePack -- pack --version X.Y.Z
   ```

   產出放在 `artifacts/release-vX.Y.Z/`（不進 git）：可離線安裝的 collection zip（URL 來源的
   plugin 取它最新的正式 tag 內嵌，可用 `--plugin-ref ss-workflow=vA.B.C` 指定其他 tag）、
   每個 plugin 各一個 zip、`SHA256SUMS.txt`，以及 `notes.md` 草稿。
3. 填好 `notes.md` 的 Changes 段，再發佈 GitHub Release：

   ```powershell
   dotnet run --project tools/ReleasePack -- upload --version X.Y.Z
   ```

   `--dry-run` 只印出 gh 指令；`--clobber` 會取代既有 release 的檔案和 notes。

工具放在 `plugins/` 之外，不會跟著任何 plugin 被安裝。
