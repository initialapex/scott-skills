# Controls, typography, spacing, custom styles

The Defaults dictionary gives every common control a Material implicit style (a plain
`<Button>` is already `MaterialDesignRaisedButton`, a plain `<TextBox>` is
`MaterialDesignTextBox`, a plain `<ToggleButton>` is a switch). Set `Style` only to pick
a different variant. Examples use the `materialDesign` XML namespace prefix and
Material Design 2 style names.

## Buttons

| Role | Style |
|------|-------|
| The main action of a screen or dialog (one per area) | `MaterialDesignRaisedButton` (default) |
| Secondary actions next to it | `MaterialDesignOutlinedButton` |
| Low-emphasis actions: Cancel, dialog buttons, actions in cards and tables | `MaterialDesignFlatButton` |
| Icon-only actions in toolbars and rows | `MaterialDesignIconButton` (always with a `ToolTip`) |
| The one creating action of a page ("New recipe") | `MaterialDesignFloatingActionButton` |
| Same roles in the secondary color | `...SecondaryButton` variants (`MaterialDesignRaisedSecondaryButton`, ...) |

```xml
<StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
    <Button Content="CANCEL" Style="{StaticResource MaterialDesignFlatButton}"
            Command="{Binding CancelCommand}" Margin="0,0,8,0" />
    <Button Content="SAVE" Command="{Binding SaveCommand}"
            materialDesign:ButtonProgressAssist.IsIndicatorVisible="{Binding SaveCommand.IsRunning}"
            materialDesign:ButtonProgressAssist.IsIndeterminate="True" />
</StackPanel>

<Button Style="{StaticResource MaterialDesignIconButton}" ToolTip="Delete"
        Command="{Binding DeleteCommand}">
    <materialDesign:PackIcon Kind="Delete" />
</Button>
```

- `ButtonProgressAssist` bound to an async command's `IsRunning` shows progress on the
  button itself; the command already disables the button while it runs.
- Enabled state comes from the command's `CanExecute`, never from `IsEnabled` bindings.
- Button text casing follows the project. Material Design 2 buttons show their text as
  written; keep one casing across the application.

## Text input

| Variant | Style | Use |
|---------|-------|-----|
| Outlined | `MaterialDesignOutlinedTextBox` / `MaterialDesignOutlinedComboBox` / `MaterialDesignOutlinedPasswordBox` | Forms and dialogs (recommended default for new screens) |
| Filled | `MaterialDesignFilledTextBox` / `MaterialDesignFilledComboBox` | Dense forms where outlines get noisy |
| Underlined, floating hint | `MaterialDesignFloatingHintTextBox` / `MaterialDesignFloatingHintComboBox` | Matching existing screens that use it |

Use one variant per application (or at least per screen).

```xml
<TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
         materialDesign:HintAssist.Hint="Recipe name"
         materialDesign:HintAssist.HelperText="Shown in the recipe list"
         materialDesign:TextFieldAssist.HasClearButton="True"
         Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}"
         MaxLength="40" />

<TextBox Style="{StaticResource MaterialDesignOutlinedTextBox}"
         materialDesign:HintAssist.Hint="Bake time"
         materialDesign:TextFieldAssist.SuffixText="min"
         Text="{Binding BakeMinutes}" />
```

- The label is `HintAssist.Hint`, not a separate `TextBlock` above the box.
- Validation errors from the ViewModel (`INotifyDataErrorInfo`, see `ss-wpf:mvvm`) are
  shown under the field by the toolkit's error template. Do not add a second error
  text.
- Units go in `TextFieldAssist.SuffixText` / `PrefixText`, icons in
  `TextFieldAssist.LeadingIcon` with `HasLeadingIcon="True"`.
- Numeric input: `materialDesign:NumericUpDown` (also `DecimalUpDown`) or a `TextBox`
  bound to a numeric property with validation.

## Selection and lists

- `CheckBox` for independent options, `ToggleButton` (switch, the default style) for
  settings that apply immediately, `RadioButton` for two to five exclusive options; a
  `ComboBox` for more.
- Lists of records: `ListBox` / `ListView` with an `ItemTemplate`; for many columns or
  editing, `DataGrid`.
- Filter chips: `ListBox` with `MaterialDesignFilterChipPrimaryListBox` and
  `SelectionMode="Multiple"`; choice chips: `MaterialDesignChoiceChipPrimaryListBox`.

## DataGrid

```xml
<DataGrid ItemsSource="{Binding Recipes}"
          SelectedItem="{Binding SelectedRecipe}"
          AutoGenerateColumns="False"
          CanUserAddRows="False"
          IsReadOnly="True"
          materialDesign:DataGridAssist.CellPadding="8,4"
          materialDesign:DataGridAssist.ColumnHeaderPadding="8,4">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="*" />
        <DataGridTextColumn Header="Bake time (min)" Binding="{Binding BakeMinutes}" />
    </DataGrid.Columns>
</DataGrid>
```

- Always `AutoGenerateColumns="False"`, with column headers written for the user.
- The default padding is roomy. For dense data views, reduce `CellPadding` and
  `ColumnHeaderPadding` in one shared style in `AppStyles.xaml`, not per grid.
- Editable grids use the toolkit's `materialDesign:DataGridTextColumn` and
  `materialDesign:DataGridComboBoxColumn` so the editors look like the other fields.

## Cards and surfaces

```xml
<materialDesign:Card Padding="16" Margin="8" UniformCornerRadius="4">
    <StackPanel>
        <TextBlock Style="{StaticResource MaterialDesignHeadline6TextBlock}" Text="{Binding Name}" />
        <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                   Foreground="{DynamicResource MaterialDesign.Brush.ForegroundLight}"
                   Text="{Binding Summary}" Margin="0,4,0,0" />
    </StackPanel>
</materialDesign:Card>
```

- Elevation: `materialDesign:ElevationAssist.Elevation="Dp2"` (values `Dp0` to `Dp24`).
  Keep to two or three levels in an application. `MaterialDesignOutlinedCard` is the
  flat alternative.
- Use a `Card` to group related content on a page, not to frame every control.

## Icons

```xml
<materialDesign:PackIcon Kind="ContentSave" Width="20" Height="20" />
```

- `Kind` comes from the Material Design Icons set (`ContentSave`, `Delete`, `Pencil`,
  `Magnify`, `Cog`, `Printer`, ...). Look a name up in the toolkit's demo application
  or `PackIconKind` before using it; a wrong name is a compile error.
- The icon takes the foreground of its parent; do not color icons individually except
  for status (error, warning).
- ViewModels never hold `PackIconKind`. When an icon depends on data, bind a string or
  an application enum and map it in XAML (a string binds to `Kind` through the enum's
  type converter), or choose the icon with a `DataTrigger`.

## Typography

Use the text styles of the Material Design version in use, so sizes and weights stay
consistent. Material Design 2:

| Role | Style |
|------|-------|
| Page title | `MaterialDesignHeadline5TextBlock` |
| Section title, card title, dialog title | `MaterialDesignHeadline6TextBlock` |
| Group heading, list primary text | `MaterialDesignSubtitle1TextBlock` |
| Body text | `MaterialDesignBody1TextBlock` (or `Body2` for dense views) |
| Captions, timestamps, small notes | `MaterialDesignCaptionTextBlock` |
| Label above a group of controls | `MaterialDesignOverlineTextBlock` |

Material Design 3 projects use `MaterialDesignHeadlineSmallTextBlock`,
`MaterialDesignTitleLargeTextBlock`, `MaterialDesignTitleMediumTextBlock`,
`MaterialDesignBodyLargeTextBlock`, `MaterialDesignBodyMediumTextBlock`,
`MaterialDesignLabelLargeTextBlock`, and so on. Do not set `FontSize` or `FontWeight`
on individual `TextBlock`s; if a size is missing, add a named style.

## Spacing

- Margins and paddings are multiples of 8 (`8`, `16`, `24`, `32`); `4` for tight spots
  such as between an icon and its text.
- Page content has `16` (dense tools) or `24` padding; cards `16`; space between fields
  in a form `8` to `16`, the same everywhere in the application.
- Repeated values become resources in `AppStyles.xaml`
  (`<Thickness x:Key="App.Margin.Field">0,0,0,16</Thickness>`) or setters in a named
  style.
- What density a screen needs (how much fits on it) is a UX decision; see
  `ss-design:ui-ux` when it is installed.

## Custom styles

Extend a toolkit style with `BasedOn`; never copy its `ControlTemplate`.

```xml
<!-- Themes/AppStyles.xaml -->
<Style x:Key="App.FormTextBox" TargetType="TextBox"
       BasedOn="{StaticResource MaterialDesignOutlinedTextBox}">
    <Setter Property="Margin" Value="{StaticResource App.Margin.Field}" />
    <Setter Property="materialDesign:TextFieldAssist.HasClearButton" Value="True" />
</Style>

<!-- Change a default for every control of a type: an implicit style based on the toolkit's -->
<Style TargetType="DataGrid" BasedOn="{StaticResource MaterialDesignDataGrid}">
    <Setter Property="materialDesign:DataGridAssist.CellPadding" Value="8,4" />
</Style>
```

- Name application styles with a prefix (`App.`) so they are not confused with toolkit
  keys.
- If a change cannot be done with setters and `*Assist` properties, say so and propose
  a custom control or a template change, with the cost (it stops following toolkit
  updates). Do not copy a template silently.
