# Moving code-behind into ViewModels

Most existing WPF projects keep their logic in `*.xaml.cs`: click handlers that read
controls by name, call devices, and write results back into controls. Do not rewrite
such a project in one go. Every change is a chance to move the part it touches, and
nothing else.

## Policy

1. **New screens and features are written in MVVM**, with the base for the project's
   environment (see [environments.md](environments.md)).
2. **Existing code moves only when a change touches it.** When a request changes a
   handler, move that handler's logic into the ViewModel as part of the change. Leave
   untouched handlers as they are.
3. **Behavior stays the same.** A move is a refactor: same inputs, same results, same
   messages to the user. Say in the answer which handlers moved, so the reviewer can
   check them.
4. **Ask before a large move.** If the touched handler pulls in a large part of the
   window (shared fields used by many handlers), propose the scope to the developer
   instead of moving the whole window silently.

## First step for a window without a ViewModel

1. Create `XxxViewModel` deriving from the environment's `ObservableObject`.
2. Set `DataContext` once: in env A from the container; in env B/C in the code that
   creates the window (`new XxxWindow { DataContext = viewModel }`), or temporarily in
   the window's constructor with a `// TODO: move to the composition root` comment.
3. Move the touched logic. The rest of the code-behind keeps working beside the
   ViewModel; both can coexist during the transition.

## Moving one handler

Before:

```csharp
private void PrintButton_Click(object sender, RoutedEventArgs e)
{
    int copies = int.Parse(CopiesTextBox.Text);
    _printer.Print(_document, copies);
    StatusLabel.Content = "Printing " + copies + " copies";
    PrintButton.IsEnabled = false;
}
```

After:

```csharp
// ViewModel (env C syntax shown; env A/B may use generators)
public int Copies
{
    get { return _copies; }
    set { SetProperty(ref _copies, value); }
}

public string StatusText
{
    get { return _statusText; }
    private set { SetProperty(ref _statusText, value); }
}

private void Print()
{
    _printService.Print(_document, Copies);   // IPrintService from the constructor
    StatusText = string.Format("Printing {0} copies", Copies);
    _isPrinting = true;
    _printCommand.NotifyCanExecuteChanged();
}

private bool CanPrint()
{
    return !_isPrinting;
}
```

```xml
<TextBox Text="{Binding Copies}" />
<Label Content="{Binding StatusText}" />
<Button Content="Print" Command="{Binding PrintCommand}" />
```

The handler and the `x:Name` attributes that only it used are deleted.

## Common shapes and where they go

| Code-behind shape | Becomes |
|-------------------|---------|
| `SomeTextBox.Text` read / written | Bound property |
| `SomeButton.IsEnabled = ...` | Command `CanExecute` |
| `Visibility = ...` from data | Bound `bool` + `BooleanToVisibilityConverter` |
| `MessageBox.Show` | `IDialogService` |
| `new OtherWindow().ShowDialog()` | `IDialogService.ShowDialogAsync` or navigation |
| Field holding a device / file / connection | Service interface injected into the ViewModel |
| `DispatcherTimer` driving data | Timer in a service or the ViewModel through an interface (`IClock`, `ITimerFactory`) so tests can drive it |
| `Dispatcher.Invoke` from a device callback | The service raises events on the UI thread, or the ViewModel uses `await` so it is already on it |
| `Loaded` handler that loads data | `OnNavigatedTo` or a command invoked from the `Loaded` event (an `EventTrigger` with `InvokeCommandAction` from `Microsoft.Xaml.Behaviors.Wpf` in env A/B, or a one-line code-behind forward in env C) |
| Mouse / drag / focus handling | Stays in code-behind (view concern) |
| Drawing on a canvas, image rendering controls | Stays in the View or a custom control; the ViewModel supplies data |

## Shared libraries

When several applications share a class library of controls and helpers, its
`INotifyPropertyChanged` base class may already exist. Reuse it instead of adding a
second one, unless it cannot raise notifications by name (`[CallerMemberName]`) or
lacks `SetProperty`; then propose replacing it, since two bases in one solution
confuse every later change.
