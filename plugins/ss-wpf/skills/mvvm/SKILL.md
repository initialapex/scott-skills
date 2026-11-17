---
name: mvvm
description: 'Design, write, refactor, and review WPF desktop code with the MVVM pattern - ViewModels, commands, bindings, DI, navigation, messaging, validation, dialogs, and ViewModel unit tests - using CommunityToolkit.Mvvm on modern .NET and .NET Framework 4.7.2/4.8, or a hand-written minimal base on old toolchains such as Visual Studio 2012. Use this whenever the work touches a WPF / XAML desktop project (a .csproj with UseWPF, or .xaml windows and user controls), even if the user does not say "MVVM": adding a window, page, dialog, or feature; moving logic out of code-behind; wiring services; making a ViewModel testable; or reviewing WPF code. Not for web front ends, WinUI, MAUI, or Avalonia, and not for visual styling or themes.'
---

# WPF MVVM

Write and review WPF code so that Views stay thin, ViewModels hold presentation logic
and can be unit tested without a window, and Models and services hold the business
logic. The same shape is used on every toolchain; only the base library changes.

## Step 1: Find the environment

Read the project's `.csproj` and `.sln` before writing code, because the environment
decides which base types, language features, and DI style are available. Details,
signals, and edge cases are in [references/environments.md](references/environments.md).

| Env | Signals | MVVM base | DI | C# |
|-----|---------|-----------|----|----|
| **A** modern .NET | SDK-style csproj, `<TargetFramework>net8.0-windows</TargetFramework>` or later, `<UseWPF>true</UseWPF>` | CommunityToolkit.Mvvm 8.x with source generators | Generic Host (`Microsoft.Extensions.Hosting`) | latest |
| **B** .NET Framework on a current toolchain | `<TargetFrameworkVersion>v4.7.2</TargetFrameworkVersion>` or v4.8 (or an SDK-style csproj targeting `net472`/`net48`), solution saved by Visual Studio 2022 | CommunityToolkit.Mvvm 8.x; source generators need `<LangVersion>` 8.0 or later | Manual composition root in `App.OnStartup` | as set in the csproj |
| **C** old toolchain | Solution saved by Visual Studio 2012 (`VisualStudioVersion = 11.0...` or `# Visual Studio 2012`), `.NET Framework 4.5` | Hand-written minimal base from [templates/legacy/](templates/legacy/) | Manual composition root in `App.OnStartup` | 5 |

If the signals disagree or are missing (for example a v4.5 project in a Visual Studio
2022 solution), ask the developer which toolchain builds it. Do not raise a project's
target framework, language version, or package format without asking: other machines
or build servers may depend on them.

## Step 2: Pick the mode

- **Design / implement**: the developer asks for a feature, window, dialog, service, or
  a refactor. Follow the core rules below and read the topic reference before writing
  that kind of code.
- **Review**: the developer asks to review or check WPF code, or a request's changes
  include WPF code. Go through
  [references/review-checklist.md](references/review-checklist.md) and report each
  finding with `file:line`, the rule it breaks, and the fix.
- **Existing code-behind project**: most older projects keep their logic in
  `*.xaml.cs`. Read [references/migration.md](references/migration.md): new code goes
  in MVVM, old code moves only when a change touches it.

## Core rules

These rules are what make a ViewModel testable and a View replaceable. When a rule has
to be broken, say so and why in the answer.

1. **The ViewModel knows nothing about the View.** No `System.Windows.Controls`,
   `Window`, `MessageBox`, `Dispatcher`, `Visibility`, `Brush`, or element names in a
   ViewModel. Anything that needs the UI goes through an interface (`IDialogService`,
   `INavigationService`) implemented in the View layer. A ViewModel may reference
   `System.Windows.Input.ICommand` and `System.ComponentModel` types, which are not UI.
2. **Code-behind holds only view concerns.** `InitializeComponent`, focus, animation,
   drag visuals, and forwarding an event that has no command to a ViewModel command.
   No business logic, no service calls, no data access.
3. **Bind, don't push.** The View reads state through bindings and acts through
   commands. Code-behind does not set control values from data.
4. **Dependencies come in through the constructor.** ViewModels and services receive
   their interfaces in the constructor; they never call `new` on a service or reach a
   static locator. The composition root (Generic Host or `App.OnStartup`) is the only
   place that creates services.
5. **Commands own their enabled state.** Use `CanExecute` (or `[RelayCommand(CanExecute
   = ...)]`) instead of binding `IsEnabled` separately, and notify the command when the
   condition changes.
6. **Async work does not block or swallow.** Long work runs in an async command, the
   UI shows its running state, it can be cancelled when it can take long, and its
   exceptions are caught and shown. Never `.Result` or `.Wait()` on the UI thread.
7. **Messaging is for loosely related ViewModels only.** A parent talks to its child
   directly; the messenger is for ViewModels that do not know each other.
8. **Every ViewModel can be created in a unit test.** If a test would need a window or
   a real device, the dependency is missing an interface.
9. **Name by role.** `XxxView` (UserControl) / `XxxWindow` (Window) pairs with
   `XxxViewModel`; services are `IXxxService` / `XxxService`; messages end in `Message`.

## Topic references

Read the one that matches the code you are about to write. Each has the pattern for
env A/B and env C.

| Topic | Reference |
|-------|-----------|
| Folder layout, naming, projects in the solution | [references/project-structure.md](references/project-structure.md) |
| ViewModel base, observable properties, commands | [references/viewmodels.md](references/viewmodels.md) |
| Composition root, DI, ViewModel lifetimes | [references/dependency-injection.md](references/dependency-injection.md) |
| Navigation (ViewModel-first) | [references/navigation.md](references/navigation.md) |
| Messenger between ViewModels | [references/messenger.md](references/messenger.md) |
| Validation (`INotifyDataErrorInfo`) | [references/validation.md](references/validation.md) |
| Async commands, cancellation, progress, errors | [references/async-commands.md](references/async-commands.md) |
| Dialogs and message boxes | [references/dialogs.md](references/dialogs.md) |
| ViewModel unit tests (MSTest, hand-written fakes) | [references/testing.md](references/testing.md) |
| Moving code-behind into ViewModels | [references/migration.md](references/migration.md) |
| Review checklist | [references/review-checklist.md](references/review-checklist.md) |

## Boundaries

- Styling, themes, colors, typography, and control templates belong to the Material
  Design skill (`ss-wpf:material-design`) when it is installed; this skill only cares
  that styling stays in XAML resources and out of ViewModels.
- General object-oriented design (class responsibilities, patterns in the Model layer)
  belongs to `ss-design:ood` when it is installed.
- Comments in generated code are in English, whatever language the conversation uses.
