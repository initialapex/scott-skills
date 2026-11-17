# Theming

## Palette

The palette is chosen by each project. This skill only fixes where it lives: one theme
dictionary, first in `App.xaml`'s merged dictionaries.

**Built-in Material colors** (`BundledTheme`):

```xml
<materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Indigo" SecondaryColor="Amber" />
```

- `PrimaryColor`: `Red`, `Pink`, `Purple`, `DeepPurple`, `Indigo`, `Blue`, `LightBlue`,
  `Cyan`, `Teal`, `Green`, `LightGreen`, `Lime`, `Yellow`, `Amber`, `Orange`,
  `DeepOrange`, `Brown`, `Grey`, `BlueGrey`, or `Inherit` (the Windows accent color).
- `SecondaryColor`: the same list without `Brown`, `Grey`, and `BlueGrey`.
- `BaseTheme`: `Light`, `Dark`, or `Inherit` (the Windows app mode when the theme is
  applied).

**Brand colors** (`CustomColorTheme`):

```xml
<materialDesign:CustomColorTheme BaseTheme="Light" PrimaryColor="#1565C0" SecondaryColor="#FF8F00" />
```

The toolkit derives the light, dark, and foreground variants from each color. If the
project already has a palette, keep it; ask before changing it.

## Theme brushes

Views use these keys (always with `DynamicResource`, so a light/dark switch updates
them):

| Key | Use |
|-----|-----|
| `MaterialDesign.Brush.Primary` / `.Primary.Light` / `.Primary.Dark` | Primary color and its variants |
| `MaterialDesign.Brush.Primary.Foreground` (also `.Light.Foreground`, `.Dark.Foreground`) | Text and icons on a primary background |
| `MaterialDesign.Brush.Secondary` (same variants) | Accents: FAB, selection, highlights |
| `MaterialDesign.Brush.Secondary.Foreground` | Text and icons on a secondary background |
| `MaterialDesign.Brush.Background` | Window and page background |
| `MaterialDesign.Brush.Foreground` | Normal text |
| `MaterialDesign.Brush.ForegroundLight` | Secondary text, hints |
| `MaterialDesign.Brush.Card.Background` | Surfaces such as cards and panels |
| `MaterialDesign.Brush.ValidationError` | Error text and borders |

```xml
<TextBlock Text="{Binding Summary}"
           Foreground="{DynamicResource MaterialDesign.Brush.ForegroundLight}" />
```

**Application brushes** that the palette does not provide (for example status colors
for OK / warning / alarm) are defined once in `Themes/AppStyles.xaml` with
application-level names (`App.Brush.Status.Ok`), and Views use those names. If a color
must differ between light and dark, define it in the code that switches the theme (see
`ThemeService` below) or pick a color readable on both, and say which in the answer.

## Light / dark switching

The ViewModel asks for a theme through an interface; the implementation in the View
layer uses the toolkit's `PaletteHelper`. Nothing in the ViewModel references the
toolkit.

```csharp
// Core / ViewModel side
public enum AppTheme
{
    Light,
    Dark,
    System
}

public interface IThemeService
{
    AppTheme Current { get; }

    void Apply(AppTheme theme);
}
```

```csharp
// Services/Ui/ThemeService.cs (View layer)
public sealed class ThemeService : IThemeService
{
    private readonly PaletteHelper _paletteHelper = new PaletteHelper();

    public ThemeService()
    {
        // Re-apply when Windows switches between light and dark while following the system.
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
    }

    public AppTheme Current { get; private set; }

    public void Apply(AppTheme theme)
    {
        Theme materialTheme = _paletteHelper.GetTheme();
        materialTheme.SetBaseTheme(ToBaseTheme(theme));
        _paletteHelper.SetTheme(materialTheme);
        Current = theme;
    }

    private static BaseTheme ToBaseTheme(AppTheme theme)
    {
        switch (theme)
        {
            case AppTheme.Dark:
                return BaseTheme.Dark;
            case AppTheme.System:
                return BaseTheme.Inherit;   // Resolved from the Windows setting now
            default:
                return BaseTheme.Light;
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && Current == AppTheme.System)
        {
            Application.Current.Dispatcher.InvokeAsync(() => Apply(AppTheme.System));
        }
    }
}
```

- `ThemeService` is a singleton (it subscribes to a static event). Register it in the
  composition root like other UI services.
- `BaseTheme.Inherit` reads the Windows setting once, when it is applied. The
  `UserPreferenceChanged` handler is what makes it follow later changes.
- The saved choice is applied at startup, before the main window is shown, so the window
  does not flash in the wrong theme:

```csharp
// App startup (env A: after the host is built; env B: in OnStartup)
themeService.Apply(settingsService.Theme);
```

The settings ViewModel drives the switch:

```csharp
public partial class SettingsViewModel : ObservableObject
{
    private readonly IThemeService _themeService;
    private readonly ISettingsService _settingsService;

    public SettingsViewModel(IThemeService themeService, ISettingsService settingsService)
    {
        _themeService = themeService;
        _settingsService = settingsService;
        _selectedTheme = themeService.Current;   // Field, so the theme is not re-applied

        Themes = new[]
        {
            new ThemeOption(AppTheme.Light, "Light"),
            new ThemeOption(AppTheme.Dark, "Dark"),
            new ThemeOption(AppTheme.System, "Use Windows setting")
        };
    }

    public IReadOnlyList<ThemeOption> Themes { get; }

    [ObservableProperty]
    private AppTheme _selectedTheme;

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _themeService.Apply(value);
        _settingsService.Theme = value;
        _settingsService.Save();
    }
}

public sealed class ThemeOption
{
    public ThemeOption(AppTheme value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public AppTheme Value { get; }
    public string DisplayName { get; }
}
```

```xml
<ComboBox Style="{StaticResource MaterialDesignOutlinedComboBox}"
          materialDesign:HintAssist.Hint="Theme"
          ItemsSource="{Binding Themes}"
          DisplayMemberPath="DisplayName"
          SelectedValuePath="Value"
          SelectedValue="{Binding SelectedTheme}"
          Width="240" />
```

In env B without generators, write `SelectedTheme` as a normal property that calls
`SetProperty` and then applies the theme. In tests, a `FakeThemeService` records the
applied theme.

A single dark region inside a light window (for example a dark side panel) uses
`materialDesign:ThemeAssist.Theme="Dark"` on that element instead of a second theme
dictionary.

## Fonts

The Material Design 2 styles use Roboto (Material Design 3 uses Noto), bundled in the
toolkit. Roboto has no Chinese, Japanese, or Korean glyphs: WPF draws those characters
with a system fallback font, so mixed text has uneven weight and baseline. Which font to
use is the project's decision; when the UI has CJK text, raise the issue and ask.

Common choices, in the developer's words:

- Keep the default and accept the fallback.
- Roboto for Latin text with a named CJK fallback: `Roboto, Microsoft JhengHei UI`
  (Traditional Chinese), `Microsoft YaHei UI` (Simplified), `Yu Gothic UI` (Japanese),
  `Malgun Gothic` (Korean).
- One CJK UI font for everything (for example `Microsoft JhengHei UI`), which keeps
  Latin and CJK text consistent at the cost of the Material look.

Wherever the font is changed, change it in one place:

```xml
<!-- Themes/AppStyles.xaml -->
<FontFamily x:Key="App.Font">pack://application:,,,/MaterialDesignThemes.Wpf;component/Resources/Roboto/#Roboto, Microsoft JhengHei UI</FontFamily>

<!-- Windows: MaterialDesignWindow sets Roboto directly, so override it in a derived style -->
<Style x:Key="AppWindowStyle" TargetType="Window" BasedOn="{StaticResource MaterialDesignWindow}">
    <Setter Property="FontFamily" Value="{StaticResource App.Font}" />
</Style>

<!-- Popups and menus do not inherit the window's font -->
<Style TargetType="ContextMenu" BasedOn="{StaticResource MaterialDesignContextMenu}">
    <Setter Property="FontFamily" Value="{StaticResource App.Font}" />
</Style>
<Style TargetType="ToolTip" BasedOn="{StaticResource MaterialDesignToolTip}">
    <Setter Property="FontFamily" Value="{StaticResource App.Font}" />
</Style>
<Style TargetType="Menu" BasedOn="{StaticResource MaterialDesignMenu}">
    <Setter Property="FontFamily" Value="{StaticResource App.Font}" />
</Style>
```

Windows then use `Style="{StaticResource AppWindowStyle}"`. A font that is not
installed on every target machine must be embedded in the application as a resource,
with its license checked.
