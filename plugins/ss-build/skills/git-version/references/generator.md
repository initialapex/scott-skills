# How the version generator behaves

`Update-GitVersion.ps1` reads the highest version tag that `HEAD` contains and writes
`.cs` and `.h` files. `GitVersion.targets` runs it before the compiler in MSBuild: for
C# it adds `obj\...\GitVersion.g.cs` to the compilation and turns off the SDK's own
version attributes; for C++ (`.vcxproj`) it writes `GitVersion.g.h` next to the project
file.

Requirements: Windows PowerShell 3.0 or later (Windows 8 and later have it; Windows 7
needs the Windows Management Framework 3.0 or later), git 1.8.5 or later, MSBuild 4.0
(Visual Studio 2010) or later. It runs on Windows only, because MSBuild starts
`powershell.exe`. With an older PowerShell the script stops with an error, and so does
the build.

## The tag has the `v`, the version it writes does not

The project's tags always start with `v` (`v1.2.3`, `v1.2.3-beta4`, `v1.2.3-rc1`); a tag
without it does not count. The generator drops the `v` and writes the rest, as .NET
versions and most version macros expect:

| State | Tag that counts | Version it writes | Numeric fields |
|-------|-----------------|-------------------|----------------|
| `HEAD` is the tagged commit | `v1.2.3` | `1.2.3` | `1.2.3.0` |
| The same, with uncommitted changes in tracked files or submodules (untracked files do not count) | `v1.2.3` | `1.2.3-dirty` | `1.2.3.0` |
| Four commits after the tag | `v1.2.3` | `1.2.3-4-g1a2b3c4` | `1.2.3.0` |
| Five commits after a prerelease tag, with changes | `v1.2.3-beta4` | `1.2.3-beta4-5-g1a2b3c4-dirty` | `1.2.3.0` |
| No version tag, a shallow clone without tags, or no git | none | `0.1.0-unknown` | `0.1.0.0` |

Only tags of the form `vX.Y.Z` or `vX.Y.Z-<alpha|beta|rc>R` count, annotated or
lightweight; of those that `HEAD` contains, the highest version wins (final > rc > beta
> alpha), not the nearest tag. The sha has 7 characters.

## What it writes

- `.cs`: `AssemblyVersion` and `AssemblyFileVersion` take the numeric fields,
  `AssemblyInformationalVersion` the full version.
- `.h`: `<PREFIX>VERSION_MAJOR`, `_MINOR`, `_PATCH`, `_BUILD` (0), `_STRING` (the full
  version), and `_NUMERIC_STRING`, also usable in a `.rc` file.
- A file is written only when its content changes, so an unchanged version does not
  trigger a rebuild. The generated files must be ignored by git; otherwise every build
  makes the version `-dirty`.

The help at the top of `Update-GitVersion.ps1` is the reference for the parameters.

## Where the files go in a project

Into the project repository, in `tools/` by default (the developer may choose another
folder):

```
<project>/
├─ App.sln
├─ App/
│  └─ App.csproj        <Import Project="..\tools\GitVersion.targets" />
├─ Driver/
│  └─ Driver.vcxproj    <Import Project="..\tools\GitVersion.targets" />
└─ tools/
   ├─ Update-GitVersion.ps1
   └─ GitVersion.targets
```

## Toolchains

| Toolchain | Support |
|-----------|---------|
| C# with MSBuild 4.0 or later, SDK-style or old-style projects | `GitVersion.targets`, imported in each project file |
| C++ with MSBuild 4.0 or later (`.vcxproj`) | `GitVersion.targets`, imported in each project file; the code and the `.rc` file include `GitVersion.g.h` |
| CMake | Not yet (planned) |
| Others (Keil, ESP-IDF, Python, Node, ...) | Not supported. A project may still take its version from tags in its own way. |
