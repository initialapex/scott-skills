# Setup

## Environments

MaterialDesignThemes 5.x ships for .NET Framework 4.6.2 and later and for
`net8.0-windows` and later, so it covers env A and env B (the same environments as
`ss-wpf:mvvm`).

Env C (Visual Studio 2012, .NET Framework 4.5) is not covered: 5.x does not target
.NET 4.5, and the old toolkit versions that did are no longer maintained. In env C, say
that this skill does not apply, keep the core rules that do not depend on the toolkit
(one resource dictionary for colors and styles, no colors in code-behind, named
styles), and write plain WPF styles.

## Install

| Env | How |
|-----|-----|
| A | `dotnet add package MaterialDesignThemes` (adds a `PackageReference`) |
| B, `PackageReference` | Same as env A, or the NuGet package manager in Visual Studio |
| B, `packages.config` | NuGet package manager in Visual Studio, or `Install-Package MaterialDesignThemes` in the Package Manager Console. Do not convert the project to `PackageReference` without asking. |

The package brings `MaterialDesignColors` and `Microsoft.Xaml.Behaviors.Wpf` with it.
Pin the same version in every project of the solution.

## App.xaml

```xml
<Application x:Class="MyApp.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <!-- 1. Palette: exactly one theme dictionary, first -->
                <materialDesign:BundledTheme BaseTheme="Light"
                                             PrimaryColor="Indigo"
                                             SecondaryColor="Amber" />

                <!-- 2. Control styles for the Material Design version in use -->
                <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />

                <!-- 3. The application's own brushes and styles, last, so they can use the above -->
                <ResourceDictionary Source="Themes/AppStyles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

- The palette (step 1) is the project's choice; see [theming.md](theming.md) for
  `BundledTheme` versus `CustomColorTheme`.
- In env A with Generic Host, `App.xaml` has no `StartupUri`; the main window comes from
  the container (see `ss-wpf:mvvm`). The resources above are unchanged.
- `Themes/AppStyles.xaml` holds the application's named brushes and styles (see
  [controls.md](controls.md)). Large applications may split it by area, merged in the
  same place.

## Windows

Every top-level window uses the toolkit's window style, so its background, foreground,
and font follow the theme:

```xml
<Window x:Class="MyApp.Views.MainWindow"
        xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
        Style="{StaticResource MaterialDesignWindow}"
        Title="My App" Height="720" Width="1200">
```

Dialog windows created by a `DialogService` use the same style. If the project needs a
different font on windows, define `AppWindowStyle` `BasedOn` `MaterialDesignWindow` in
`AppStyles.xaml` (see [theming.md](theming.md#fonts)) and use that everywhere instead.

## Material Design 2 or 3

| `App.xaml` merges | Version | Text styles | Default font |
|-------------------|---------|-------------|--------------|
| `MaterialDesign2.Defaults.xaml` | Material Design 2 | `MaterialDesignHeadline5TextBlock`, `MaterialDesignSubtitle1TextBlock`, `MaterialDesignBody1TextBlock`, ... | Roboto |
| `MaterialDesign3.Defaults.xaml` | Material Design 3 | `MaterialDesignHeadlineSmallTextBlock`, `MaterialDesignTitleMediumTextBlock`, `MaterialDesignBodyLargeTextBlock`, ... | Noto |

- New projects use Material Design 2: its controls are complete and best documented.
- A project that already uses Material Design 3 stays on it. Use the MD3 text styles
  and the MD3 navigation styles, and check that a style exists in the MD3 dictionaries
  before using it.
- Do not use MD3-only styles in an MD2 application or the other way round. Switching
  versions is a project decision, not part of a feature change.

## Projects on MaterialDesignThemes 4.x

Keep 4.x unless the developer agrees to upgrade; the upgrade touches every window. When
it is agreed, these are the changes to plan for:

| 4.x | 5.x |
|-----|-----|
| `MaterialDesignTheme.Defaults.xaml` | `MaterialDesign2.Defaults.xaml` (or `MaterialDesign3.Defaults.xaml`) |
| Brush keys `PrimaryHueMidBrush`, `PrimaryHueMidForegroundBrush`, `SecondaryHueMidBrush`, `MaterialDesignPaper`, `MaterialDesignBody`, `MaterialDesignBodyLight`, ... | `MaterialDesign.Brush.Primary`, `MaterialDesign.Brush.Primary.Foreground`, `MaterialDesign.Brush.Secondary`, `MaterialDesign.Brush.Background`, `MaterialDesign.Brush.Foreground`, `MaterialDesign.Brush.ForegroundLight`, ... |
| `...Accent...` styles (`MaterialDesignRaisedAccentButton`, `MaterialDesignFlatAccentButton`, ...) | `...Secondary...` styles (`MaterialDesignRaisedSecondaryButton`, `MaterialDesignFlatSecondaryButton`, ...) |
| `materialDesign:ShadowAssist.ShadowDepth="Depth2"` | `materialDesign:ElevationAssist.Elevation="Dp2"` |

The old brush keys and Accent styles exist in 5.x only in the
`MaterialDesignTheme.ObsoleteBrushes.xaml` and `MaterialDesignTheme.ObsoleteStyles.xaml`
dictionaries, which are not merged by default. Rename the usages instead of merging the
obsolete dictionaries, and search all `.xaml` files for the old names before building.

When a 4.x project stays on 4.x, write XAML with the 4.x names and say in the answer
which 5.x names they will become.
