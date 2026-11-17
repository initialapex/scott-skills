# Environments

The pattern is the same everywhere. What changes is which library provides
`ObservableObject` and the commands, which C# features compile, and how services are
wired.

## How to tell them apart

Check in this order and stop at the first match.

1. **The `.sln` header.** Open the solution file:
   - `# Visual Studio 2012`, or `VisualStudioVersion = 11.0.x`: **env C**.
   - `VisualStudioVersion = 17.x` (Visual Studio 2022) or later: env A or B, decided by
     the project file.
   - `VisualStudioVersion = 12.x`-`16.x` (2013-2019): not covered by a fixed
     environment. Ask the developer; treat it as env C unless they confirm a
     toolchain that runs source generators (see "Env B" below).
   - The header records the Visual Studio version that last saved the solution, not
     the oldest one that must still open it. If the developer says a team member or a
     build server still uses Visual Studio 2012, it is env C even with a 2022 header.
2. **The `.csproj`.**
   - SDK-style (`<Project Sdk="Microsoft.NET.Sdk">`) with `net8.0-windows` or later:
     **env A**.
   - SDK-style targeting `net472` / `net48`: **env B**.
   - Old-style (`<TargetFrameworkVersion>`): v4.7.2 or v4.8 is **env B**; v4.5 / v4.5.x
     is **env C**; v4.6.x is env B only after the developer agrees, because
     CommunityToolkit.Mvvm targets .NET Standard 2.0, which is unreliable below 4.7.2.
3. **`<LangVersion>`** in the csproj, if present, limits the C# you may write in env B
   and C. Without it, an old-style .NET Framework project compiles as C# 7.3.

## Env A: modern .NET

- Package: `CommunityToolkit.Mvvm` 8.x, `Microsoft.Extensions.Hosting`.
- Use the source generators: `[ObservableProperty]` on private fields,
  `[RelayCommand]` on methods, `[NotifyPropertyChangedFor]`,
  `[NotifyCanExecuteChangedFor]`. Classes that use them are `partial`.
- `[ObservableProperty]` on a field works on every supported SDK; prefer it unless the
  project already uses partial properties.
- Use file-scoped namespaces and nullable reference types if the project has them on;
  follow the project's existing style when it differs.

## Env B: .NET Framework 4.7.2 / 4.8 with Visual Studio 2022

- Same package and the same generator attributes as env A.
- **Language version.** The toolkit's generators require C# 8.0 or later and report an
  error on 7.3 (the default for old-style .NET Framework projects). Ask before adding
  `<LangVersion>8.0</LangVersion>` (or later) to the csproj. If the developer declines,
  use the toolkit without generators: derive from `ObservableObject`, write properties
  with `SetProperty(ref field, value)`, and create `RelayCommand` /
  `AsyncRelayCommand` instances in the constructor.
- C# 8+ features that need runtime support (default interface members, `Index` /
  `Range` on arrays) do not work on .NET Framework even with a higher `LangVersion`;
  the generated code does not use them.
- **packages.config vs PackageReference.** Generators are delivered as analyzers. With
  `PackageReference` they are picked up automatically. With `packages.config`, check
  that NuGet added `<Analyzer Include="...CommunityToolkit.Mvvm.SourceGenerators.dll" />`
  items to the csproj after the install; if a build reports that a generated member
  does not exist, that is the cause. Migrating the project to `PackageReference`
  fixes it, but ask first.
- DI: a manual composition root (see
  [dependency-injection.md](dependency-injection.md)), no Generic Host.
- C#: whatever `LangVersion` allows. With 7.3: no nullable reference types, no
  switch expressions, no `using` declarations.

## Env C: Visual Studio 2012 / .NET Framework 4.5 / C# 5

- CommunityToolkit.Mvvm cannot be used (Visual Studio 2012's NuGet cannot consume
  .NET Standard packages). Copy the hand-written base from
  [../templates/legacy/](../templates/legacy/) into the project, under a `Mvvm`
  folder, and change its namespace to the project's root namespace plus `.Mvvm`.
  The class and member names match CommunityToolkit.Mvvm, so a later upgrade to env B
  is a namespace change.
- DI: a manual composition root, no container.
- **C# 5 only.** Not available:
  - `nameof` (C# 6): use `[CallerMemberName]` for property names, string literals
    elsewhere.
  - `?.` and `??=`: write explicit null checks.
  - String interpolation `$"..."`: use `string.Format`.
  - Expression-bodied members `=>`, auto-property initializers, getter-only
    auto-properties: write full properties and assign in the constructor.
  - `using static`, exception filters (`catch ... when`), `await` in `catch` /
    `finally`, tuples, pattern matching, local functions, `out var`.
- Available: `async` / `await`, `Task.Run`, `CancellationToken`, `IProgress<T>`,
  `[CallerMemberName]`, `INotifyDataErrorInfo`, LINQ, generics, lambdas.
- Not in .NET 4.5: `Task.CompletedTask` (use `Task.FromResult(0)`),
  `Task.FromException`, `Array.Empty<T>()`.
- Tests: the Visual Studio 2012 "Unit Test Project" template
  (`Microsoft.VisualStudio.QualityTools.UnitTestFramework`). It supports
  `async Task` test methods.
