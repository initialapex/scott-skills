# Dialogs on DialogHost, notifications on Snackbar

ViewModels keep using the `IDialogService` contract from `ss-wpf:mvvm`
(`ShowMessageAsync`, `ShowErrorAsync`, `ConfirmAsync`, `PickOpenFileAsync`,
`PickSaveFileAsync`, `ShowDialogAsync(IDialogViewModel)`). In a MaterialDesignThemes
application its implementation shows dialogs in the window's `DialogHost` (an overlay
inside the window) instead of separate windows. Short notifications go through a
separate `ISnackbarService`. ViewModels and their tests do not change.

## When to use which

| Situation | Use |
|-----------|-----|
| A decision before a destructive or costly action | `ConfirmAsync` (dialog) |
| An error the user must read (load failed, device not ready) | `ShowErrorAsync` (dialog) |
| A small form (rename, enter a value) | `ShowDialogAsync` with a dialog ViewModel |
| Feedback that an action finished ("Recipe saved"), optionally with Undo | `ISnackbarService` |
| Choosing a file | `PickOpenFileAsync` / `PickSaveFileAsync` (the Windows file dialogs) |

Never put an error that needs action in a Snackbar: it disappears after a few seconds.

## Shell XAML

The `DialogHost` wraps the whole content of the shell window, so the overlay covers
everything (see [shell.md](shell.md) for the full window):

```xml
<materialDesign:DialogHost Identifier="RootDialog" DialogTheme="Inherit">
    <Grid>
        <!-- app bar, drawer, page host ... -->

        <materialDesign:Snackbar x:Name="MainSnackbar"
                                 HorizontalAlignment="Stretch"
                                 VerticalAlignment="Bottom" />
    </Grid>
</materialDesign:DialogHost>
```

The `DataTemplate`s that map dialog ViewModels to their Views sit in the application
resources, next to the page templates, so the `DialogHost` finds them.

## IDialogService on DialogHost (`Services/Ui/DialogHostDialogService.cs`)

```csharp
public sealed class DialogHostDialogService : IDialogService
{
    // Must match the Identifier of the DialogHost in the shell window.
    private const string HostIdentifier = "RootDialog";

    public Task ShowMessageAsync(string title, string message)
    {
        return ShowAsync(new MessageDialogViewModel(title, message, MessageKind.Information, "OK", null));
    }

    public Task ShowErrorAsync(string title, string message)
    {
        return ShowAsync(new MessageDialogViewModel(title, message, MessageKind.Error, "OK", null));
    }

    public Task<bool> ConfirmAsync(string title, string message)
    {
        return ShowAsync(new MessageDialogViewModel(title, message, MessageKind.Question, "OK", "CANCEL"));
    }

    public Task<bool> ShowDialogAsync(IDialogViewModel viewModel)
    {
        return ShowAsync(viewModel);
    }

    // PickOpenFileAsync / PickSaveFileAsync: same as the window-based DialogService
    // (OpenFileDialog / SaveFileDialog with the active window as owner).

    private static async Task<bool> ShowAsync(IDialogViewModel viewModel)
    {
        EventHandler<DialogCloseRequestedEventArgs> onClose = (sender, e) =>
        {
            if (DialogHost.IsDialogOpen(HostIdentifier))
            {
                DialogHost.Close(HostIdentifier, e.Result);
            }
        };

        viewModel.CloseRequested += onClose;
        try
        {
            // The content is the ViewModel; its DataTemplate supplies the View.
            object result = await DialogHost.Show(viewModel, HostIdentifier);
            return result is bool ok && ok;   // Click-away or Esc closes with null = cancel
        }
        finally
        {
            viewModel.CloseRequested -= onClose;
        }
    }
}
```

- Register it as the `IDialogService` singleton instead of the window-based one. The
  ViewModel tests keep using `FakeDialogService`.
- One dialog at a time per `DialogHost`: `DialogHost.Show` throws if the host is
  already open. A dialog that needs a confirmation of its own either validates inline
  (preferred) or opens a second, nested `DialogHost` with its own identifier inside the
  dialog View.
- The identifier ties the service to one window. Secondary windows that need dialogs
  use the window-based `DialogService`, or give each window its own identifier and
  pass it in; say which in the answer.
- `CloseOnClickAway="True"` on the `DialogHost` lets a click outside cancel; leave it
  off for dialogs that ask for input, so typed text is not lost by a stray click.

## Message dialog

```csharp
// ViewModels/Dialogs/MessageDialogViewModel.cs
public enum MessageKind
{
    Information,
    Question,
    Error
}

public partial class MessageDialogViewModel : ObservableObject, IDialogViewModel
{
    public MessageDialogViewModel(string title, string message, MessageKind kind, string okText, string cancelText)
    {
        Title = title;
        Message = message;
        Kind = kind;
        OkText = okText;
        CancelText = cancelText;
    }

    public string Title { get; }
    public string Message { get; }
    public MessageKind Kind { get; }
    public string OkText { get; }
    public string CancelText { get; }   // null hides the cancel button
    public bool HasCancel => CancelText != null;

    public event EventHandler<DialogCloseRequestedEventArgs> CloseRequested;

    [RelayCommand]
    private void Ok() => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false));
}
```

```xml
<!-- Views/Dialogs/MessageDialogView.xaml -->
<UserControl x:Class="MyApp.Views.Dialogs.MessageDialogView"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             MinWidth="320" MaxWidth="480">
    <StackPanel Margin="24">
        <StackPanel Orientation="Horizontal">
            <materialDesign:PackIcon x:Name="KindIcon" Kind="InformationOutline"
                                     Width="24" Height="24" Margin="0,0,16,0"
                                     VerticalAlignment="Center" />
            <TextBlock Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                       Text="{Binding Title}" VerticalAlignment="Center" />
        </StackPanel>

        <TextBlock Style="{StaticResource MaterialDesignBody1TextBlock}"
                   Text="{Binding Message}" TextWrapping="Wrap" Margin="0,16,0,24" />

        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right">
            <Button Style="{StaticResource MaterialDesignFlatButton}"
                    Content="{Binding CancelText}" Command="{Binding CancelCommand}"
                    IsCancel="True" Margin="0,0,8,0"
                    Visibility="{Binding HasCancel, Converter={StaticResource BooleanToVisibilityConverter}}" />
            <Button Style="{StaticResource MaterialDesignFlatButton}"
                    Content="{Binding OkText}" Command="{Binding OkCommand}" IsDefault="True" />
        </StackPanel>
    </StackPanel>
</UserControl>
```

In env B without generators, create `OkCommand` and `CancelCommand` in the constructor
with `RelayCommand`, as in `ss-wpf:mvvm`.

Map `Kind` to the icon with `DataTrigger`s in a style on the `PackIcon`
(`Question` → `HelpCircleOutline`, `Error` → `AlertCircleOutline` with
`MaterialDesign.Brush.ValidationError` as foreground). `BooleanToVisibilityConverter` is
the WPF converter declared once in the application resources.

Dialog buttons are flat (`MaterialDesignFlatButton`), confirm on the right, cancel on
its left.

## Form dialogs

Form dialogs (rename, settings for one item) are dialog ViewModels as in
`ss-wpf:mvvm`; only their View changes: 24 padding, a `MaterialDesignHeadline6TextBlock`
title, outlined text fields, flat buttons at the bottom right. Validation and the OK
command's `CanExecute` stay in the ViewModel.

## Snackbar

```csharp
// Core / ViewModel side
public interface ISnackbarService
{
    void Show(string message);

    void Show(string message, string actionText, Action action);
}
```

```csharp
// Services/Ui/SnackbarService.cs
public sealed class SnackbarService : ISnackbarService
{
    private readonly ISnackbarMessageQueue _queue;

    public SnackbarService(ISnackbarMessageQueue queue)
    {
        _queue = queue;
    }

    public void Show(string message)
    {
        _queue.Enqueue(message);
    }

    public void Show(string message, string actionText, Action action)
    {
        _queue.Enqueue(message, actionText, action);
    }
}
```

Wiring:

```csharp
// Env A (Generic Host): one queue for the application; the host disposes it.
services.AddSingleton(_ => new SnackbarMessageQueue(TimeSpan.FromSeconds(3)) { DiscardDuplicates = true });
services.AddSingleton<ISnackbarMessageQueue>(sp => sp.GetRequiredService<SnackbarMessageQueue>());
services.AddSingleton<ISnackbarService, SnackbarService>();

// Env B (manual composition root in OnStartup); dispose the queue in OnExit.
_snackbarQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3)) { DiscardDuplicates = true };
ISnackbarService snackbarService = new SnackbarService(_snackbarQueue);
```

The shell window receives the same queue and hands it to its `Snackbar` (whose
`MessageQueue` property takes the concrete `SnackbarMessageQueue`). This is a View
concern, so it is fine in code-behind:

```csharp
public MainWindow(MainViewModel viewModel, SnackbarMessageQueue snackbarQueue)
{
    InitializeComponent();
    DataContext = viewModel;
    MainSnackbar.MessageQueue = snackbarQueue;
}
```

ViewModel use:

```csharp
await _recipeService.DeleteAsync(recipe);
Recipes.Remove(item);
_snackbarService.Show("Recipe deleted", "UNDO", () => UndoDeleteCommand.Execute(recipe));
```

- `Enqueue` may be called from any thread; the queue shows messages on its dispatcher.
- `DiscardDuplicates = true` (off by default) keeps repeated clicks from queuing the
  same message several times.
- Tests use a `FakeSnackbarService` that records the messages.
