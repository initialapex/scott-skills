# Validation

Validate in the ViewModel with `INotifyDataErrorInfo`. WPF bindings read it by default
(`ValidatesOnNotifyDataErrors=True` is the default since .NET 4.5) and show the error
template on the bound control. Do not validate in code-behind or with
`ValidationRule` classes in XAML for business rules.

## Env A / B: `ObservableValidator`

```csharp
public partial class RecipeEditorViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyDataErrorInfo]                       // validate on every change
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(40, ErrorMessage = "Name is at most 40 characters.")]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyDataErrorInfo]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    [Range(20.0, 300.0, ErrorMessage = "Temperature must be between 20 and 300 °C.")]
    private double _temperature = 20.0;

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            return;
        }

        await _recipeService.SaveAsync(new Recipe(Name, Temperature));
    }

    private bool CanSave() => !HasErrors;
}
```

- Without generators, write the property with
  `SetProperty(ref _name, value, validate: true)`.
- Cross-field or service-based rules: `[CustomValidation(typeof(RecipeEditorViewModel), nameof(ValidateName))]`
  with a `public static ValidationResult ValidateName(string name, ValidationContext context)`
  method; `context.ObjectInstance` is the ViewModel.

## Env C: template `ObservableValidator`

Copy [../templates/legacy/ObservableValidator.cs](../templates/legacy/ObservableValidator.cs)
(add a reference to `System.ComponentModel.DataAnnotations`). The same attributes work;
the property validates in its setter:

```csharp
[Required(ErrorMessage = "Name is required.")]
[MaxLength(40, ErrorMessage = "Name is at most 40 characters.")]
public string Name
{
    get { return _name; }
    set
    {
        if (SetProperty(ref _name, value, true))
        {
            _saveCommand.NotifyCanExecuteChanged();
        }
    }
}
```

Call `ValidateAllProperties()` before saving, then check `HasErrors`.

## XAML

```xml
<TextBox x:Name="NameBox" Text="{Binding Name, UpdateSourceTrigger=PropertyChanged}" />
```

- The default red border is the WPF error template; replace it with a style (the
  Material Design skill owns that) instead of drawing errors manually.
- To show the message under the field:
  `<TextBlock Text="{Binding (Validation.Errors)[0].ErrorContent, ElementName=NameBox}" />`
  only when the style does not already show it.

## Rules

- **When to validate**: per change for simple fields, so the user sees the error as
  they type; on submit for the rest (`ValidateAllProperties()` in the save command).
  Do not show errors on a form the user has not touched yet: start with
  `ClearErrors()`, or validate only from the first edit.
- **What the ViewModel validates**: input format and screen-level rules. Business
  invariants (a recipe name must be unique) live in the Model or service; the
  ViewModel shows the error the service returns.
- **Save is disabled while there are errors**, through `CanExecute`, not through
  `IsEnabled` in XAML.
- **Error messages** are user-facing text; keep them in one place (attributes or a
  resource file), not built in code-behind.
