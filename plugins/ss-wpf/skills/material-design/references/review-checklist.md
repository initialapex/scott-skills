# Review checklist

Review the changed XAML and View-layer code (or the files the developer named) against
these items. Report each finding as:

```
<file>:<line>  [<item id>] <what is wrong>  →  <the fix>
```

Group findings by severity: **must fix** (breaks theming, light/dark switching, or the
build), **should fix** (inconsistencies that will spread), **consider** (polish). Say
which toolkit version and Material Design version (2 or 3) you reviewed against.
Existing screens that the request did not change are reported only when they block the
change.

The ids start with `MD-` so they do not collide with the `ss-wpf:mvvm` checklist when
both are used in one review.

## MD-T: Theme and colors

- MD-T1 `App.xaml` has exactly one theme dictionary (`BundledTheme` or
  `CustomColorTheme`), first, followed by the Defaults dictionary of one Material Design
  version. *(must)*
- MD-T2 No window, page, or control merges a theme or Defaults dictionary of its own.
  *(must)*
- MD-T3 No hard-coded colors (`#RRGGBB`, `Colors.X`, named brushes like `Red`) in
  Views; theme brushes or the application's named brushes are used. *(should)*
- MD-T4 Theme brushes are referenced with `DynamicResource`. *(must)*
- MD-T5 No brushes or colors are set from code-behind or ViewModels. *(must)*
- MD-T6 Theme switching goes through `IThemeService`; no `PaletteHelper` in ViewModels.
  *(must)*
- MD-T7 Application colors the palette does not provide are defined once in the
  application's resource dictionary. *(should)*

## MD-C: Controls and styles

- MD-C1 Toolkit styles and `*Assist` properties are used before custom styles; custom
  styles are `BasedOn` a toolkit style. *(should)*
- MD-C2 No copied toolkit `ControlTemplate` without a stated reason. *(should)*
- MD-C3 Repeated setters on several controls are a named style in a resource
  dictionary. *(should)*
- MD-C4 One text field variant (outlined, filled, or floating hint) per screen, and
  labels are `HintAssist.Hint`, not separate `TextBlock`s. *(should)*
- MD-C5 One main (raised) button per area; dialog buttons are flat; icon-only buttons
  have a `ToolTip`. *(should)*
- MD-C6 No obsolete names in a 5.x project: `...Accent...` styles, `PrimaryHueMidBrush`
  and other 4.x brush keys, `ShadowAssist.ShadowDepth`. *(must)*
- MD-C7 `DataGrid` has `AutoGenerateColumns="False"` and readable headers. *(should)*
- MD-C8 Enabled state comes from commands, not `IsEnabled` bindings, and async buttons
  show progress through `ButtonProgressAssist` or a bound indicator. *(should)*
- MD-C9 `PackIcon` kinds exist in the toolkit version in use. *(must)*

## MD-Y: Typography and spacing

- MD-Y1 Text uses the toolkit's text styles of the Material Design version in use; no
  `FontSize` / `FontWeight` on individual `TextBlock`s. *(should)*
- MD-Y2 Margins and paddings are multiples of 8 (4 for tight spots) and repeated values
  are resources. *(consider)*
- MD-Y3 When the UI has CJK text, the font decision is made in one place (window style
  and popup styles), not per control. *(should)*

## MD-D: Dialogs and notifications

- MD-D1 ViewModels use `IDialogService` and `ISnackbarService`; no `DialogHost`,
  `SnackbarMessageQueue`, or `MessageBox` in ViewModels. *(must)*
- MD-D2 The `DialogHost` identifier used by the dialog service matches the shell's
  `DialogHost`. *(must)*
- MD-D3 Errors that need action are dialogs, not Snackbar messages. *(should)*
- MD-D4 No dialog is opened on a `DialogHost` that may already be open (nested
  confirmation inside a dialog). *(must)*
- MD-D5 Input dialogs do not close on click-away. *(consider)*

## MD-L: Shell and layout

- MD-L1 Windows use `MaterialDesignWindow` or a style based on it. *(should)*
- MD-L2 The `DialogHost` wraps the whole window content. *(should)*
- MD-L3 Navigation items are ViewModel data and navigate through `INavigationService`;
  no `PackIconKind` or View types in the shell ViewModel. *(must)*
- MD-L4 Pages own their padding and scrolling; the shell does not wrap every page in a
  `ScrollViewer`. *(consider)*

## MD-E: Environment

- MD-E1 Env C (Visual Studio 2012 / .NET 4.5) projects do not get MaterialDesignThemes
  code; plain WPF styles are used. *(must)*
- MD-E2 The toolkit major version, the Material Design version (2 or 3), and the
  palette were not changed without the developer's agreement. *(must)*
- MD-E3 All projects in the solution reference the same MaterialDesignThemes version.
  *(should)*
- MD-E4 Styles of Material Design 2 and 3 are not mixed in one application. *(must)*
