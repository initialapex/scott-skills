# Dependency injection and the composition root

The composition root is the single place that creates services and the first window.
Every other class receives what it needs through its constructor.

Remove `StartupUri` from `App.xaml` in every environment: the composition root opens
the first window itself.

## Env A: Generic Host

```csharp
// App.xaml.cs
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services: singletons unless they hold per-screen state.
                services.AddSingleton<IRecipeService, RecipeService>();
                services.AddSingleton<IDialogService, DialogService>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

                // Shell: one instance for the application's lifetime.
                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainViewModel>();

                // Page and dialog ViewModels: a new instance each time they are shown.
                services.AddTransient<RecipeListViewModel>();
                services.AddTransient<RecipeEditorViewModel>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = _host.Services.GetRequiredService<MainViewModel>();
        window.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
```

- Only the composition root, `NavigationService`, and `DialogService` touch
  `IServiceProvider`; ViewModels never do (that would be a service locator).
- A ViewModel that needs to create other ViewModels on demand receives a factory
  (`Func<RecipeEditorViewModel>` registered as
  `services.AddSingleton<Func<RecipeEditorViewModel>>(sp => () => sp.GetRequiredService<RecipeEditorViewModel>())`)
  or goes through `INavigationService`.
- Configuration (`appsettings.json`) and logging (`ILogger<T>`) come with the host; use
  them instead of static settings classes.

## Env B and C: manual composition root

No container. `App.OnStartup` creates the object graph by hand, in dependency order.

```csharp
// App.xaml.cs (C# 5 compatible; env B may use newer syntax if LangVersion allows)
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Services: one instance each, shared by everything below.
        IMessenger messenger = WeakReferenceMessenger.Default;
        IRecipeService recipeService = new RecipeService();
        IDialogService dialogService = new DialogService();

        // Factories for ViewModels that are created each time they are shown.
        Func<RecipeListViewModel> recipeListFactory =
            delegate { return new RecipeListViewModel(recipeService, messenger); };
        Func<RecipeEditorViewModel> recipeEditorFactory =
            delegate { return new RecipeEditorViewModel(recipeService, dialogService, messenger); };

        NavigationService navigationService = new NavigationService();
        navigationService.Register(recipeListFactory);
        navigationService.Register(recipeEditorFactory);

        MainViewModel mainViewModel = new MainViewModel(navigationService);
        navigationService.AttachShell(mainViewModel);

        MainWindow window = new MainWindow();
        window.DataContext = mainViewModel;
        MainWindow = window;
        window.Show();

        navigationService.NavigateTo<RecipeListViewModel>();
    }
}
```

- Keep the composition root in `App.xaml.cs`, or in a `Bootstrapper` class called from
  it when it grows past one screen.
- `WeakReferenceMessenger.Default` is the toolkit's in env B and the template's in
  env C; pass it as `IMessenger` so tests can give a fresh instance.
- Services that hold a device or file open implement `IDisposable`; dispose them in
  `App.OnExit`.

## Lifetimes

| Kind | Lifetime | Why |
|------|----------|-----|
| Stateless services, device and settings services | One instance | They represent one resource |
| Shell ViewModel (`MainViewModel`) | One instance | Lives as long as the main window |
| Page ViewModels | New per navigation, unless the page must keep its state | Fresh state, no stale subscriptions |
| Dialog ViewModels | New per dialog | A dialog starts empty each time |
| Item ViewModels | Created by their parent ViewModel with `new` | They wrap data, not services |

## Testability check

If a ViewModel constructor takes a concrete class that touches hardware, files, the
network, or a window, extract an interface for it. The test project then passes a
hand-written fake (see [testing.md](testing.md)).
