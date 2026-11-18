# scott-skills

Scott Chang's Claude Code plugin marketplace.

[繁體中文](README-zh-TW.md)

## Plugins

| Plugin | Source | Description |
|--------|--------|-------------|
| `ss-workflow` | [initialapex/ss-workflow](https://github.com/initialapex/ss-workflow) (separate repository) | Request-driven gitflow workflow: init, new/check request, review, merge, release |
| `ss-wpf` | [plugins/ss-wpf](plugins/ss-wpf) | WPF desktop applications. `ss-wpf:mvvm`: MVVM with CommunityToolkit.Mvvm on .NET 8+ and .NET Framework 4.7.2/4.8, or a hand-written base for Visual Studio 2012. `ss-wpf:material-design`: MaterialDesignThemes (MaterialDesignInXaml) 5.x themes, styles, dialogs, and shell for .NET 8+ and .NET Framework 4.7.2/4.8 |
| `taste-skill` | [leonxlnx/taste-skill](https://github.com/leonxlnx/taste-skill) (third party, MIT) | Art direction, reference images, and a browser check for professional company websites. `taste-skill:taste-skill` |
| `ui-skills` | [ibelick/ui-skills](https://github.com/ibelick/ui-skills) (third party, MIT) | Skills for design engineers: UI cleanup and accessibility, metadata, and motion performance fixes. `ui-skills:baseline-ui`, `ui-skills:fixing-accessibility`, and more |
| `document-skills`, `cc-skills`, `llm-api`, `academy-guide`, `discernment-nudge` | [anthropics/skills](https://github.com/anthropics/skills) (third party, Apache-2.0) | The plugins of Anthropic's skills repository: Office and PDF documents; skill-creator, frontend-design, and other practical skills; Claude API reference; and more. Two plugins are renamed from upstream: `llm-api` is `claude-api` (third-party marketplaces cannot use the `claude-` prefix), and `cc-skills` is `example-skills` (the name reads like a sample) |
| `ss-design` | [plugins/ss-design](plugins/ss-design) | Technology-independent design. `ss-design:ood`: object-oriented design (responsibilities, SOLID, design patterns, domain model, code smells and refactoring, error handling) with Mermaid diagrams and a review checklist; C# 7.3 examples. `ss-design:ui-ux`: UI and UX design that first decides between easy to use and easy to learn (information architecture and flows, layout, forms, tables and feedback, keyboard and accessibility, localization, field operation screens) with Mermaid flows, ASCII wireframes, and a review checklist |
| `ss-build` | [plugins/ss-build](plugins/ss-build) | Build and release helpers for Windows projects (C# and C++ with MSBuild, Windows PowerShell). `ss-build:git-version`: the version comes from git tags, with a generator that writes it into ignored files and the conversion of a project to it. `ss-build:release-package`: one script that zips the release build, one zip per platform |

Every plugin of this marketplace is prefixed `ss-` so that its skill namespace
`<plugin>:<skill>` does not collide with plugins from other marketplaces.

## Install

Run inside a Claude Code session:

```
/plugin marketplace add initialapex/scott-skills
/plugin install ss-workflow@scott-skills
/plugin install ss-wpf@scott-skills
/plugin install ss-design@scott-skills
/plugin install ss-build@scott-skills
/plugin install taste-skill@scott-skills
/plugin install ui-skills@scott-skills
/plugin install document-skills@scott-skills
/plugin install cc-skills@scott-skills
```

The third-party plugins are listed by GitHub repository and follow its default branch.

### Offline

Each [GitHub Release](https://github.com/initialapex/scott-skills/releases) has a
`scott-skills-vX.Y.Z.zip` that holds the whole marketplace, with `ss-workflow` and the
third-party plugins (`taste-skill`, `ui-skills`, `anthropics/skills`) bundled.
Extract it and add the extracted folder as a local marketplace:

```
/plugin marketplace add C:\path\to\scott-skills-vX.Y.Z
/plugin install ss-workflow@scott-skills
/plugin install ss-wpf@scott-skills
/plugin install ss-design@scott-skills
/plugin install ss-build@scott-skills
/plugin install taste-skill@scott-skills
/plugin install ui-skills@scott-skills
/plugin install document-skills@scott-skills
/plugin install cc-skills@scott-skills
```

## Layout

```
.claude-plugin/marketplace.json   lists every plugin
plugins/<plugin>/                 plugins that live in this repository
tools/ReleasePack/                release packaging tool (not part of any plugin)
tests/                            checks of the ss-build scripts (bash tests/git-version.sh, bash tests/release-package.sh; need Windows PowerShell)
```

Plugins with their own release cycle (like `ss-workflow`) and third-party plugins stay
in their own repository and are listed here by GitHub repository or URL. Third-party
plugins keep their upstream names (except where a name is reserved) and are not
modified; the bundle keeps their license files. Plugins that live here are listed with a
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

## Releasing

1. Open `release/vX.Y.Z` from `develop`, set `metadata.version` in
   `marketplace.json` (and the `version` of changed plugins), merge it into `main`
   with the tags, merge it back into `develop`, and push `main`, `develop`, and the
   tags.
2. Package the release from the tag (needs the .NET 10 SDK, git, and gh; the claude
   CLI validates the package when it is installed):

   ```powershell
   dotnet run --project tools/ReleasePack -- pack --version X.Y.Z
   ```

   This writes to `artifacts/release-vX.Y.Z/` (ignored by git): the collection zip
   for offline installation, with every remote plugin bundled from its latest
   release tag (`--plugin-ref ss-workflow=vA.B.C` picks another tag; a repository
   without release tags, like `anthropics/skills`, is bundled from its default
   branch and the commit is recorded in `notes.md`), one zip per plugin (plugins
   of one repository share a zip), `SHA256SUMS.txt`, and a `notes.md` draft.
3. Fill in the Changes section of `notes.md`, then publish the GitHub Release:

   ```powershell
   dotnet run --project tools/ReleasePack -- upload --version X.Y.Z
   ```

   `--dry-run` prints the gh commands; `--clobber` replaces the assets and notes of
   an existing release.

The tool lives outside `plugins/`, so it is never installed with a plugin.
