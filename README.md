# scott-skills

Scott Chang's Claude Code plugin marketplace.

[繁體中文](README-zh-TW.md)

## Plugins

| Plugin | Source | Description |
|--------|--------|-------------|
| `ss-workflow` | [initialapex/ss-workflow](https://github.com/initialapex/ss-workflow) (separate repository) | Request-driven gitflow workflow: init, new/check request, review, merge, release |
| `ss-wpf` | [plugins/ss-wpf](plugins/ss-wpf) | WPF desktop applications. `ss-wpf:mvvm`: MVVM with CommunityToolkit.Mvvm on .NET 8+ and .NET Framework 4.7.2/4.8, or a hand-written base for Visual Studio 2012. `ss-wpf:material-design`: MaterialDesignThemes (MaterialDesignInXaml) 5.x themes, styles, dialogs, and shell for .NET 8+ and .NET Framework 4.7.2/4.8 |
| `ss-design` | [plugins/ss-design](plugins/ss-design) | Technology-independent design. `ss-design:ood`: object-oriented design (responsibilities, SOLID, design patterns, domain model, code smells and refactoring, error handling) with Mermaid diagrams and a review checklist; C# 7.3 examples. `ss-design:ui-ux`: UI and UX design that first decides between easy to use and easy to learn (information architecture and flows, layout, forms, tables and feedback, keyboard and accessibility, localization, field operation screens) with Mermaid flows, ASCII wireframes, and a review checklist |

Every plugin of this marketplace is prefixed `ss-` so that its skill namespace
`<plugin>:<skill>` does not collide with plugins from other marketplaces.

## Install

Run inside a Claude Code session:

```
/plugin marketplace add initialapex/scott-skills
/plugin install ss-workflow@scott-skills
/plugin install ss-wpf@scott-skills
/plugin install ss-design@scott-skills
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

- The collection carries its own version in `metadata.version` of
  `.claude-plugin/marketplace.json`; each release of the repository bumps it and is
  tagged `vX.Y.Z`.
- Each plugin carries its own `version` in its `.claude-plugin/plugin.json`.
- Plugins in this repository are tagged `<plugin>/vX.Y.Z` when their version changes
  in a release.
- Releases go through a `release/vX.Y.Z` branch named after the collection version.
- `main` holds released versions only; daily work goes to `develop`.
