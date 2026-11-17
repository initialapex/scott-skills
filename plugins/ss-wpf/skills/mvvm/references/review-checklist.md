# Review checklist

Review the changed WPF code (or the files the developer named) against these items.
Report each finding as:

```
<file>:<line>  [<item id>] <what is wrong>  →  <the fix>
```

Group findings by severity: **must fix** (breaks testability, threading, or leaks),
**should fix** (rule violations that will hurt later), **consider** (style). Say which
environment (A/B/C) you reviewed against. Code that was not changed in this request is
reported only when it blocks the change, following [migration.md](migration.md).

## V: View and code-behind

- V1 Code-behind has no business logic, service calls, or data access. *(must)*
- V2 Code-behind does not set control values from data; it binds. *(should)*
- V3 Page Views do not set their own `DataContext`; shells and dialogs get it from the
  composition root or `DialogService`. *(should)*
- V4 Every binding path exists on the ViewModel (check `d:DataContext` design
  instance, or search the ViewModel). Misspelled paths fail silently. *(must)*
- V5 Styling is in XAML resources, not in code-behind or the ViewModel. *(should)*

## M: ViewModel

- M1 No reference to `System.Windows.Controls`, `Window`, `MessageBox`, `Dispatcher`,
  `Visibility`, `Brush`, or element names. *(must)*
- M2 Dependencies arrive through the constructor as interfaces; no `new` on services,
  no static service locator, no `App.Current` access. *(must)*
- M3 Properties notify: generated `[ObservableProperty]`, or `SetProperty` in env B/C.
  No field writes that skip notification after construction. *(must)*
- M4 Dependent properties and commands are refreshed when their source changes
  (`NotifyPropertyChangedFor`, `NotifyCanExecuteChangedFor`, or explicit calls). *(must)*
- M5 Commands are created once and exposed get-only as `IRelayCommand` /
  `IAsyncRelayCommand`. *(should)*
- M6 Enabled state comes from `CanExecute`, not a parallel `IsXxxEnabled` property bound
  to `IsEnabled`. *(should)*
- M7 Lists are `ObservableCollection<T>`, changed on the UI thread. *(must)*
- M8 Getters are cheap and have no side effects. *(should)*

## A: Async and threading

- A1 No `.Result`, `.Wait()`, or `GetAwaiter().GetResult()` on the UI thread. *(must)*
- A2 No `async void` outside event handlers; command methods return `Task`. *(must)*
- A3 Expected exceptions are caught in the command method and shown to the user. *(must)*
- A4 Long operations accept a `CancellationToken` and pass it down; cancellation is not
  reported as an error. *(should)*
- A5 `Task.Run` only wraps CPU work and does not touch ViewModel state inside. *(must)*
- A6 `App` has `DispatcherUnhandledException` and `TaskScheduler.UnobservedTaskException`
  handlers. *(should)*

## S: Services, DI, navigation, dialogs, messenger

- S1 Services sit behind interfaces; device, file, and network access is only in
  services. *(must)*
- S2 The composition root is the only place that wires services; lifetimes follow
  [dependency-injection.md](dependency-injection.md). *(should)*
- S3 Navigation goes through `INavigationService` by ViewModel type; DataTemplates map
  ViewModels to Views. *(should)*
- S4 Dialogs and file pickers go through `IDialogService`. *(must)*
- S5 Messenger handlers use the recipient argument and do not capture `this`; the
  ViewModel unregisters when it goes away. *(must)*
- S6 The messenger is not used between a parent and its child or between a ViewModel
  and a service. *(should)*

## R: Validation

- R1 Input is validated in the ViewModel through `INotifyDataErrorInfo`
  (`ObservableValidator`). *(should)*
- R2 Save is blocked while there are errors, through `CanExecute`. *(should)*
- R3 Business invariants are enforced in the Model or service, not only in the
  ViewModel. *(should)*

## T: Tests

- T1 New or changed ViewModel behavior has MSTest tests with hand-written fakes. *(should)*
- T2 Tests await `ExecuteAsync`, do not sleep, and do not touch files, devices, or
  windows. *(must)*

## E: Environment

- E1 The code compiles in the project's environment: no generator attributes without
  CommunityToolkit.Mvvm and LangVersion 8+; only C# 5 in env C (no `nameof`, `?.`,
  `$"..."`, `=>` members, auto-property initializers). *(must)*
- E2 Target framework, LangVersion, and package format were not changed without the
  developer's agreement. *(must)*
- E3 Only one MVVM base exists in the solution. *(should)*
