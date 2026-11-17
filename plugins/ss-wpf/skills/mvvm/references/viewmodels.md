# ViewModels, properties, and commands

## Env A / B with source generators

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class RecipeEditorViewModel : ObservableObject
{
    private readonly IRecipeService _recipeService;

    public RecipeEditorViewModel(IRecipeService recipeService)
    {
        _recipeService = recipeService;
    }

    // Generates the public property Name; SaveCommand re-queries CanSave when Name changes.
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private string _name = string.Empty;

    // Generates Temperature and also raises PropertyChanged for Summary.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    private double _temperature;

    public string Summary => $"{Name}: {Temperature:F1} °C";

    // Generates SaveCommand (IAsyncRelayCommand); disabled while running or when CanSave is false.
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        await _recipeService.SaveAsync(new Recipe(Name, Temperature));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    // Partial hook generated for every [ObservableProperty]; runs after the value changed.
    partial void OnTemperatureChanged(double value)
    {
        // React to the change here instead of in the setter.
    }
}
```

- The class must be `partial`. Fields are `_camelCase` or `camelCase`; the generated
  property is `PascalCase`.
- Never write to the field after construction; assign the generated property so that
  notifications fire. (The constructor may set the field to avoid an early notification.)
- `On<Property>Changing` / `On<Property>Changed` partial methods replace custom setter
  logic.
- For a command with a parameter, give the method one parameter:
  `[RelayCommand] private void Delete(Recipe recipe)` generates `DeleteCommand`
  (`IRelayCommand<Recipe>`).

## Env B without generators (LangVersion below 8)

```csharp
public class RecipeEditorViewModel : ObservableObject
{
    private readonly IRecipeService _recipeService;
    private string _name = string.Empty;

    public RecipeEditorViewModel(IRecipeService recipeService)
    {
        _recipeService = recipeService;
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
    }

    public string Name
    {
        get { return _name; }
        set
        {
            if (SetProperty(ref _name, value))
            {
                SaveCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public IAsyncRelayCommand SaveCommand { get; }

    private async Task SaveAsync() { /* ... */ }

    private bool CanSave() { return !string.IsNullOrWhiteSpace(Name); }
}
```

## Env C (hand-written base, C# 5)

Copy the files in [../templates/legacy/](../templates/legacy/) first. The ViewModel is the
same as the env B example, with C# 5 syntax:

```csharp
using MyApp.Mvvm;

namespace MyApp.ViewModels
{
    public class RecipeEditorViewModel : ObservableObject
    {
        private readonly IRecipeService _recipeService;
        private readonly AsyncRelayCommand _saveCommand;
        private string _name;

        public RecipeEditorViewModel(IRecipeService recipeService)
        {
            _recipeService = recipeService;
            _name = string.Empty;
            _saveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
        }

        public string Name
        {
            get { return _name; }
            set
            {
                if (SetProperty(ref _name, value))
                {
                    _saveCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public IAsyncRelayCommand SaveCommand
        {
            get { return _saveCommand; }
        }

        private async Task SaveAsync()
        {
            await _recipeService.SaveAsync(new Recipe(Name));
        }

        private bool CanSave()
        {
            return !string.IsNullOrWhiteSpace(Name);
        }
    }
}
```

- A dependent property raises its own notification from the setter of the property it
  depends on: `OnPropertyChanged("Summary");` (no `nameof` in C# 5).

## Rules for every environment

- **Commands are created once**, in the constructor (or by the generator), and exposed
  as get-only properties typed as the interface (`IRelayCommand`, `IAsyncRelayCommand`).
- **Collections** shown in lists are `ObservableCollection<T>` exposed as a get-only
  property; change their content, do not replace the instance, unless the whole list
  is reloaded (then raise the property change).
- **Collections are changed on the UI thread.** After `await` in a ViewModel method you
  are back on the UI thread, so add items there; do not add from `Task.Run`.
- **Item ViewModels.** When list items have their own state or commands, wrap each
  Model in an `XxxItemViewModel` instead of adding UI state to the Model.
- **No logic in property getters** beyond returning a field or a cheap computation;
  getters run on every binding refresh.
- **Bindings** in XAML use `Mode=TwoWay` only where the user edits the value, and
  `UpdateSourceTrigger=PropertyChanged` only when the ViewModel must react per
  keystroke.
- **Design-time data**: set `d:DataContext="{d:DesignInstance Type=vm:RecipeEditorViewModel, IsDesignTimeCreatable=False}"`
  on the View so the designer and IntelliSense know the binding paths.
