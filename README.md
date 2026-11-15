# scott-skills

Scott Chang's Claude Code plugin marketplace.

[繁體中文](README-zh-TW.md)

## Plugins

| Plugin | Source | Description |
|--------|--------|-------------|
| `ss-workflow` | [initialapex/ss-workflow](https://github.com/initialapex/ss-workflow) (separate repository) | Request-driven gitflow workflow: init, new/check request, review, merge, release |

Planned (every plugin of this marketplace is prefixed `ss-` so that its skill namespace
`<plugin>:<skill>` does not collide with plugins from other marketplaces):

| Plugin | Skills | Scope |
|--------|--------|-------|
| `ss-design` | `ood`, `ui-ux` | Technology-independent design principles and review checklists |
| `ss-wpf` | `mvvm`, `material-design` | WPF: CommunityToolkit.Mvvm and MaterialDesignInXaml |

## Install

Run inside a Claude Code session:

```
/plugin marketplace add initialapex/scott-skills
/plugin install ss-workflow@scott-skills
```

## Layout

```
.claude-plugin/marketplace.json   lists every plugin
plugins/<plugin>/                 plugins that live in this repository
```

Plugins with their own release cycle (like `ss-workflow`) stay in their own
repository and are listed here by URL. Plugins that live here are listed with a
relative path (`./plugins/<plugin>`).

## Versioning

- Each plugin carries its own `version` in its `.claude-plugin/plugin.json`.
- Plugins in this repository are tagged `<plugin>/vX.Y.Z`.
- `main` holds released versions only; daily work goes to `develop`.
