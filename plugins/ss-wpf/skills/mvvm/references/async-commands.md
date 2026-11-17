# Async commands

Anything that can take more than a moment (device I/O, files, network, heavy
computation) runs in an `AsyncRelayCommand`. The command disables itself while it
runs, so double clicks do not start a second run.

## Env A / B with generators

```csharp
// Generates LoadCommand (IAsyncRelayCommand) and, with IncludeCancelCommand,
// LoadCancelCommand, which is enabled only while LoadCommand runs.
[RelayCommand(IncludeCancelCommand = true)]
private async Task LoadAsync(CancellationToken token)
{
    try
    {
        StatusText = "Loading...";
        var recipes = await _recipeService.LoadAllAsync(token);   // I/O stays async
        Recipes.Clear();                                           // back on the UI thread
        foreach (var recipe in recipes)
        {
            Recipes.Add(new RecipeItemViewModel(recipe));
        }

        StatusText = $"{Recipes.Count} recipes";
    }
    catch (OperationCanceledException)
    {
        StatusText = "Cancelled";
    }
    catch (IOException ex)
    {
        await _dialogService.ShowErrorAsync("Load failed", ex.Message);
    }
}
```

```xml
<Button Content="Load" Command="{Binding LoadCommand}" />
<Button Content="Cancel" Command="{Binding LoadCancelCommand}" />
<ProgressBar IsIndeterminate="True"
             Visibility="{Binding LoadCommand.IsRunning, Converter={StaticResource BoolToVisibilityConverter}}" />
```

## Env B without generators, and env C

```csharp
// Constructor
_loadCommand = new AsyncRelayCommand(LoadAsync);            // Func<CancellationToken, Task>
_cancelLoadCommand = new RelayCommand(_loadCommand.Cancel, () => _loadCommand.CanBeCanceled);
_loadCommand.PropertyChanged += OnLoadCommandPropertyChanged;

private void OnLoadCommandPropertyChanged(object sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == "CanBeCanceled")
    {
        _cancelLoadCommand.NotifyCanExecuteChanged();
    }
}
```

The `LoadAsync(CancellationToken token)` method is the same as above, with C# 5 syntax
in env C (`string.Format` instead of `$"..."`).

## CPU-bound work

```csharp
var summary = await Task.Run(() => _reportBuilder.Build(orders, token), token);
```

- Use `Task.Run` only for CPU work. I/O APIs that are already async are awaited
  directly; wrapping them in `Task.Run` only wastes a thread.
- Code inside `Task.Run` must not touch ViewModel properties or collections; return a
  result and assign it after the `await`.
- Progress from a background loop: create `var progress = new Progress<int>(p => ProgressValue = p);`
  on the UI thread and pass `IProgress<int>` into the work. `Progress<T>` posts back
  to the UI thread for you.

## Rules

- **Never block**: no `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on the UI
  thread. It freezes the window and can deadlock.
- **No `async void`** except event handlers. Command methods return `Task`.
- **Catch expected exceptions in the command method** and show them through
  `IDialogService` or a status property. An exception that escapes is rethrown on the
  UI thread and ends in the application's unhandled exception handler.
- **Always have a last-resort handler** in `App`: `DispatcherUnhandledException` (log,
  show a message, set `e.Handled = true` only if the application can continue) and
  `TaskScheduler.UnobservedTaskException` (log).
- **Cancellation** is offered for anything the user might wait on for more than a few
  seconds. Pass the token all the way down to the I/O call, and treat
  `OperationCanceledException` as a normal outcome, not an error.
- **State while running**: bind busy indicators to `XxxCommand.IsRunning` instead of a
  separate `IsBusy` flag, unless several commands share one indicator.
- **Leaving the page** (`OnNavigatedFrom`) or closing the window cancels running
  commands of that ViewModel.
- **Fire-and-forget** work started from a constructor is not allowed; start loading
  from `OnNavigatedTo` or a `Loaded` event forwarded to a command.
