---
name: material-design
description: 'Style, theme, lay out, and review WPF desktop UIs with MaterialDesignThemes (MaterialDesignInXaml Toolkit 5.x) - App.xaml theme setup, palettes, runtime light/dark switching, Material buttons, text fields, combo boxes, data grids, cards, icons, typography, DialogHost dialogs, Snackbar notifications, and the app shell (ColorZone app bar, DrawerHost, navigation rail). Use this whenever a WPF project references MaterialDesignThemes or materialDesign: XAML, or the user wants a WPF window or control to look Material, changes colors, themes, fonts, icons, or styles, builds a dialog or notification in such a project, or asks to review its XAML styling. Not for MVVM structure without visual work, and not for web, WinUI, MAUI, or Avalonia.'
---

# WPF Material Design (MaterialDesignThemes)

Make WPF screens look and behave like Material Design with the MaterialDesignThemes
package (the MaterialDesignInXaml Toolkit), while keeping every visual decision in XAML
resources so a theme change touches one place. This skill covers the visual layer; the
ViewModels behind it follow `ss-wpf:mvvm`.

## Step 1: Check the environment and the toolkit version

Read the `.csproj`, `packages.config` (if any), and `App.xaml` first. Details are in
[references/setup.md](references/setup.md).

| Env | Signals | This skill |
|-----|---------|------------|
| **A** modern .NET | SDK-style csproj, `net8.0-windows` or later, `<UseWPF>true</UseWPF>` | MaterialDesignThemes 5.x |
| **B** .NET Framework on a current toolchain | `v4.7.2` / `v4.8` (or `net472` / `net48`), Visual Studio 2022 solution | MaterialDesignThemes 5.x |
| **C** old toolchain | Visual Studio 2012 solution, .NET Framework 4.5 | Not covered. Say so, and use plain WPF styles in a resource dictionary instead. |

Then find what the project already uses:

- **Toolkit version**: the `MaterialDesignThemes` package version. A 4.x project keeps
  4.x until the developer agrees to upgrade (see setup.md for what changes).
- **Material Design 2 or 3**: `App.xaml` merges `MaterialDesign2.Defaults.xaml` or
  `MaterialDesign3.Defaults.xaml`. New projects use Material Design 2. Follow whichever
  the project uses, and never mix the two style families in one application.
- **Palette**: `BundledTheme` or `CustomColorTheme` in `App.xaml`. The palette belongs
  to the project; do not change it unless asked.

## Step 2: Pick the mode

- **Set up**: add the toolkit to a project, or create the shell. Read
  [references/setup.md](references/setup.md) and [references/shell.md](references/shell.md).
- **Design / implement**: style a screen, add a dialog or notification, change the
  theme. Follow the core rules and read the topic reference first.
- **Review**: the developer asks to review or check the UI or XAML, or a request's
  changes include XAML in a MaterialDesignThemes project. Go through
  [references/review-checklist.md](references/review-checklist.md) and report each
  finding with `file:line`, the rule it breaks, and the fix.

## Core rules

1. **The theme is set once, in `App.xaml`.** One `BundledTheme` or `CustomColorTheme`,
   then the Defaults dictionary for the project's Material Design version. Windows and
   controls never merge theme dictionaries of their own.
2. **No hard-coded colors in Views.** Use the theme brushes
   (`MaterialDesign.Brush.Primary`, `MaterialDesign.Brush.Foreground`, ...) or the
   project's own named brushes defined in one resource dictionary.
3. **Theme brushes are `DynamicResource`.** `StaticResource` keeps the old color after a
   light/dark switch.
4. **Use the toolkit's styles before writing your own.** Pick the named style
   (`MaterialDesignOutlinedTextBox`, `MaterialDesignFlatButton`, ...) and the `*Assist`
   attached properties. A custom style is `BasedOn` a toolkit style, never a copied
   template.
5. **Styles live in resource dictionaries, not in code-behind or ViewModels.** Repeated
   setters on several controls become a named style in the window's or the
   application's resources.
6. **ViewModels stay free of toolkit types.** No `PaletteHelper`, `DialogHost`,
   `SnackbarMessageQueue`, `PackIconKind`, or brushes in a ViewModel; they go behind
   `IThemeService`, `IDialogService`, and `ISnackbarService` in the View layer.
7. **Typography uses the toolkit's text styles** for the Material Design version in
   use, so sizes stay consistent; spacing uses multiples of 8 (4 for tight spots).
8. **Do not raise the toolkit's major version, switch Material Design 2 and 3, or
   replace the palette without the developer's agreement.**

## Topic references

| Topic | Reference |
|-------|-----------|
| Install, `App.xaml`, window style, 4.x projects, MD2 / MD3 | [references/setup.md](references/setup.md) |
| Palette, theme brushes, light/dark switching, fonts and CJK text | [references/theming.md](references/theming.md) |
| Buttons, text fields, lists, data grids, cards, icons, typography, spacing, custom styles | [references/controls.md](references/controls.md) |
| `IDialogService` on `DialogHost`, Snackbar notifications | [references/dialogs-snackbar.md](references/dialogs-snackbar.md) |
| Shell window: app bar, drawer, navigation rail, page host | [references/shell.md](references/shell.md) |
| Review checklist | [references/review-checklist.md](references/review-checklist.md) |

## Boundaries

- ViewModels, commands, DI, navigation, and the `IDialogService` contract follow
  `ss-wpf:mvvm` when it is installed. This skill only supplies the visual
  implementations (DialogHost, Snackbar, shell XAML).
- Layout and density principles (what goes on a screen, how dense a data view should
  be, flows) belong to `ss-design:ui-ux` when it is installed; this skill covers how to
  express them with the toolkit.
- Comments in generated code are in English, whatever language the conversation uses.
