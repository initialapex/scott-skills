# Project structure

Follow the layout the project already has. Use this layout for a new project, and as
the target when a feature is added to a project that has no clear layout.

## Solution

```
MyApp.sln
├─ MyApp/                    WPF application (exe): Views, ViewModels, composition root
├─ MyApp.Core/               optional class library: Models, service interfaces, logic
│                            with no WPF reference, shared with tests and tools
├─ MyApp.Controls/           optional WPF class library: reusable controls, converters,
│                            behaviors, styles shared by several applications
└─ MyApp.Tests/              MSTest project: ViewModel and service tests
```

- Start with the single application project plus the test project. Split out `Core`
  when a second consumer appears (a second app, a command line tool) or when the
  developer wants the logic compiled without WPF.
- The test project references the application project (or `Core`); it never needs a
  running window.

## Folders in the application project

```
MyApp/
├─ App.xaml / App.xaml.cs    composition root (DI, first window), global exception handlers
├─ Views/                    XxxView.xaml (UserControl), XxxWindow.xaml (Window)
├─ ViewModels/               XxxViewModel.cs, one per View
├─ Models/                   domain types and data objects
├─ Services/                 IXxxService.cs + XxxService.cs (devices, files, settings)
│  └─ Ui/                    services implemented with WPF: DialogService, NavigationService
├─ Messages/                 messenger message types (XxxMessage)
├─ Converters/               IValueConverter implementations (XxxToYyyConverter)
├─ Resources/                styles, templates, images, string resources
└─ Mvvm/                     env C only: the hand-written base types
```

- When a feature is large (several Views, ViewModels, and services that belong
  together), a feature folder (`Features/Recipe/` holding its View, ViewModel, and
  service) is fine; do not mix both layouts inside one project.
- Keep the namespace equal to the folder path (`MyApp.ViewModels`), as Visual Studio
  generates it.

## Naming

| Kind | Name | Example |
|------|------|---------|
| UserControl | `XxxView` | `RecipeListView` |
| Window | `XxxWindow` | `SettingsWindow` |
| ViewModel | `XxxViewModel`, same `Xxx` as its View | `RecipeListViewModel` |
| Service interface / implementation | `IXxxService` / `XxxService`; a role name (`IRecipeStore` / `FileRecipeStore`) is fine when it says more | `IRecipeService` / `RecipeService` |
| Message | `XxxMessage` (past tense for events) | `RecipeSavedMessage` |
| Converter | `XxxToYyyConverter` | `BoolToVisibilityConverter` |
| Command property | verb + `Command` | `SaveCommand` |
| Async command method (generator) | verb + `Async` (the generated command drops `Async`) | `SaveAsync` → `SaveCommand` |

## Where code goes

| Code | Place |
|------|-------|
| What is shown, what is enabled, what a button does | ViewModel |
| How it looks: layout, styles, visual states, animations | XAML (View, Resources) |
| Rules and calculations that do not depend on the screen | Model or service |
| Device, file, network, database access | Service behind an interface |
| Converting a value for display only (bool to Visibility) | Converter in XAML |
| Opening windows, message boxes, file pickers | `Services/Ui`, behind an interface |
