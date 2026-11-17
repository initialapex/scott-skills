# Dialogs and message boxes

ViewModels never call `MessageBox.Show`, `new SomeWindow().ShowDialog()`,
`OpenFileDialog`, or `SaveFileDialog`. They call `IDialogService`, which the View layer
implements. This keeps ViewModels testable: a test fake answers the dialog.

## Interface

Return `Task` so the same interface fits modal windows and in-window (overlay)
dialogs, such as Material Design's `DialogHost`.

```csharp
public interface IDialogService
{
    Task ShowMessageAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
    Task<bool> ConfirmAsync(string title, string message);

    // Returns null when the user cancels.
    Task<string> PickOpenFileAsync(string filter);
    Task<string> PickSaveFileAsync(string filter, string defaultFileName);

    // Shows the View mapped to the ViewModel (DataTemplate) in a modal window.
    // Returns true when the dialog closed with OK.
    Task<bool> ShowDialogAsync(IDialogViewModel viewModel);
}

public interface IDialogViewModel
{
    string Title { get; }

    // Raised by the dialog ViewModel to close itself; true = OK, false = cancel.
    event EventHandler<DialogCloseRequestedEventArgs> CloseRequested;
}

public sealed class DialogCloseRequestedEventArgs : EventArgs
{
    public DialogCloseRequestedEventArgs(bool result)
    {
        Result = result;
    }

    public bool Result { get; private set; }
}
```

In env C, `Task.FromResult(...)` returns completed tasks from the synchronous
implementations.

## Implementation (`Services/Ui/DialogService.cs`)

```csharp
public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message)
    {
        MessageBoxResult answer = MessageBox.Show(GetOwner(), message, title,
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(answer == MessageBoxResult.Yes);
    }

    public Task<string> PickOpenFileAsync(string filter)
    {
        OpenFileDialog dialog = new OpenFileDialog();   // Microsoft.Win32
        dialog.Filter = filter;
        bool? ok = dialog.ShowDialog(GetOwner());
        return Task.FromResult(ok == true ? dialog.FileName : null);
    }

    public Task<bool> ShowDialogAsync(IDialogViewModel viewModel)
    {
        // DialogWindow is a generic Window whose content is a ContentControl bound to
        // its DataContext; the DataTemplate for the ViewModel supplies the View.
        DialogWindow window = new DialogWindow();
        window.Owner = GetOwner();
        window.Title = viewModel.Title;
        window.DataContext = viewModel;

        EventHandler<DialogCloseRequestedEventArgs> onClose = null;
        onClose = delegate(object sender, DialogCloseRequestedEventArgs e)
        {
            viewModel.CloseRequested -= onClose;
            window.DialogResult = e.Result;
        };
        viewModel.CloseRequested += onClose;

        bool? result = window.ShowDialog();
        viewModel.CloseRequested -= onClose;
        return Task.FromResult(result == true);
    }

    // ShowMessageAsync, ShowErrorAsync, PickSaveFileAsync follow the same pattern.

    private static Window GetOwner()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window.IsActive)
            {
                return window;
            }
        }

        return Application.Current.MainWindow;
    }
}
```

The `DialogWindow.xaml` has `SizeToContent="WidthAndHeight"`,
`WindowStartupLocation="CenterOwner"`, `ShowInTaskbar="False"`, and a single
`<ContentControl Content="{Binding}" />`.

## Dialog ViewModels

```csharp
public partial class RenameViewModel : ObservableObject, IDialogViewModel
{
    public string Title => "Rename";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(OkCommand))]
    private string _newName = string.Empty;

    public event EventHandler<DialogCloseRequestedEventArgs> CloseRequested;

    [RelayCommand(CanExecute = nameof(CanOk))]
    private void Ok() => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(true));

    private bool CanOk() => !string.IsNullOrWhiteSpace(NewName);

    [RelayCommand]
    private void Cancel() => CloseRequested?.Invoke(this, new DialogCloseRequestedEventArgs(false));
}
```

The caller reads the result from the dialog ViewModel after `ShowDialogAsync` returns:

```csharp
var rename = new RenameViewModel { NewName = Selected.Name };
if (await _dialogService.ShowDialogAsync(rename))
{
    Selected.Name = rename.NewName;
}
```

## Rules

- One `IDialogService` for the application; add methods to it instead of creating a
  service per dialog, until it grows past about ten methods, then split by area.
- Confirmation before destructive actions (delete, overwrite, discard changes) goes
  through `ConfirmAsync` in the ViewModel, so the test can check both answers.
- A dialog ViewModel does not know its window; it closes itself with `CloseRequested`.
- Window size, position, and owner are View concerns, set in `DialogService` or the
  window's XAML.
