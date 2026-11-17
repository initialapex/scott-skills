# Navigation (ViewModel-first)

The shell shows whatever ViewModel is current; WPF picks the View through a
`DataTemplate`. ViewModels navigate by ViewModel type, never by View, so navigation is
unit-testable and the View layer can change without touching ViewModels.

## Pieces

1. **Shell ViewModel** with a `CurrentViewModel` property.
2. **Shell window** with a `ContentControl` bound to it.
3. **DataTemplates** mapping each page ViewModel type to its View.
4. **`INavigationService`** that creates the target ViewModel and sets
   `CurrentViewModel`.
5. Optional **`INavigationAware`** for pages that need a parameter or cleanup.

## Interface (all environments)

```csharp
public interface INavigationService
{
    // Creates the ViewModel and makes it current; parameter is passed to INavigationAware.
    void NavigateTo<TViewModel>(object parameter = null) where TViewModel : class;
}

public interface INavigationAware
{
    void OnNavigatedTo(object parameter);
    void OnNavigatedFrom();
}
```

The shell ViewModel exposes the current page:

```csharp
// Env A/B with generators. Env C: a normal property with SetProperty.
public partial class MainViewModel : ObservableObject
{
    public MainViewModel(INavigationService navigationService)
    {
        NavigateToRecipesCommand = new RelayCommand(() => navigationService.NavigateTo<RecipeListViewModel>());
    }

    [ObservableProperty]
    private object _currentViewModel;

    public IRelayCommand NavigateToRecipesCommand { get; }
}
```

## Implementation

The service sets the shell's `CurrentViewModel`. It lives in `Services/Ui` because it
knows the shell; ViewModels see only the interface.

**Env A** (resolves ViewModels from the container):

```csharp
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _services;
    private readonly Lazy<MainViewModel> _shell;

    public NavigationService(IServiceProvider services)
    {
        _services = services;
        // Lazy breaks the cycle MainViewModel -> INavigationService -> MainViewModel.
        _shell = new Lazy<MainViewModel>(() => services.GetRequiredService<MainViewModel>());
    }

    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : class
    {
        (_shell.Value.CurrentViewModel as INavigationAware)?.OnNavigatedFrom();

        var target = _services.GetRequiredService<TViewModel>();
        _shell.Value.CurrentViewModel = target;
        (target as INavigationAware)?.OnNavigatedTo(parameter);
    }
}
```

**Env B / C** (factories registered by the composition root; C# 5 syntax):

```csharp
public sealed class NavigationService : INavigationService
{
    private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();
    private MainViewModel _shell;

    public void Register<TViewModel>(Func<TViewModel> factory) where TViewModel : class
    {
        _factories[typeof(TViewModel)] = factory;
    }

    // Called once by the composition root after the shell exists.
    public void AttachShell(MainViewModel shell)
    {
        _shell = shell;
    }

    public void NavigateTo<TViewModel>(object parameter = null) where TViewModel : class
    {
        INavigationAware current = _shell.CurrentViewModel as INavigationAware;
        if (current != null)
        {
            current.OnNavigatedFrom();
        }

        object target = _factories[typeof(TViewModel)]();
        _shell.CurrentViewModel = target;

        INavigationAware aware = target as INavigationAware;
        if (aware != null)
        {
            aware.OnNavigatedTo(parameter);
        }
    }
}
```

In the manual composition root, call `navigationService.AttachShell(mainViewModel)`
right after creating `MainViewModel`.

## XAML

```xml
<!-- App.xaml or a merged ResourceDictionary (Resources/ViewTemplates.xaml) -->
<Application.Resources>
    <DataTemplate DataType="{x:Type vm:RecipeListViewModel}">
        <views:RecipeListView />
    </DataTemplate>
    <DataTemplate DataType="{x:Type vm:RecipeEditorViewModel}">
        <views:RecipeEditorView />
    </DataTemplate>
</Application.Resources>

<!-- MainWindow.xaml -->
<ContentControl Content="{Binding CurrentViewModel}" />
```

- The View gets its `DataContext` from the template automatically. Do not set
  `DataContext` in the page View's XAML or code-behind.
- Keep all ViewModel-to-View templates in one dictionary so a missing one is easy to
  spot (a missing template shows the ViewModel's type name on screen).

## Rules

- `OnNavigatedFrom` unregisters messenger subscriptions and cancels running work of the
  page being left.
- Back navigation, when needed, is a stack of ViewModels inside the service; keep the
  stack small, or recreate pages instead of keeping them.
- A navigation menu binds to commands on the shell ViewModel, not to click handlers.
- Wizards and tabbed areas use the same pattern with their own `CurrentViewModel` (a
  child ViewModel acting as a local shell).
