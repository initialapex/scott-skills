# How the package script behaves

`New-ReleasePackage.ps1` runs with Windows PowerShell 3.0 or later and .NET Framework
4.5 or later. The help at the top of the file is the reference for its parameters. In
short:

- It reads `package.psd1` next to it, a PowerShell data file (see the example in
  SKILL.md). `From` is relative to the repository root, with wildcards; a folder brings
  its files with their subfolders. `To` is the folder in the zip. `{Platform}` stands for
  each entry of `Platforms`. The patterns point at the `Release` output: a package is
  always made from the release build.
- **Platforms are listed, not discovered.** With no `Platforms` or one entry, the run
  writes `<Name>-v<version>.zip`; with several, `<Name>-v<version>-<platform>.zip` for
  each. The solution may define more platforms than the project ships (`Any CPU`,
  `Mixed Platforms`); only the listed ones are packaged. The build command must build
  each of them: a pattern that matches nothing fails the run.
- **The version**: `-Version`, else the highest version tag on `HEAD` (the zip drops the
  `v`), else, when `Update-GitVersion.ps1` is next to it, the generator's version
  (`1.2.3-4-g1a2b3c4` after a tag). Without any of them it fails.
- **It refuses** uncommitted changes in tracked files or submodules (untracked files do
  not count), a missing version, an unknown placeholder, and two different files with
  the same name in the zip. It writes no zip when any package of the run fails.
- It writes into `dist/` of the repository root by default (`-OutputDirectory` to change
  it), which the project's `.gitignore` must list, and prints each zip as
  `Package: <full path>`. **That line is the contract with a release**: a workflow that
  runs the package command reads the `Package:` lines to find the zips, one per
  platform, and checks that each name contains `-v<version>`.

## Where the files go in a project

```
<project>/
├─ .gitignore              dist/
└─ tools/
   ├─ New-ReleasePackage.ps1
   └─ package.psd1
```

`New-ReleasePackage.ps1` is copied unchanged and carries its version in the line
`ss-build-script: New-ReleasePackage <version>`. `package.psd1` belongs to the project
and is never overwritten by an update.
