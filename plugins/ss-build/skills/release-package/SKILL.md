---
name: release-package
description: 'Make the release zip of a Windows project that ships binaries with one script instead of by hand - set up New-ReleasePackage.ps1 and its package.psd1 (which files go into the zip, one zip per platform such as x86 and x64) in a project, change what goes into the package, add a platform, or update the copied script to the current version. Use this whenever the user wants a repeatable release package, says the zip is made by hand or differs between people or machines, wants one zip per platform, wants the CI or the release to produce the same zip as the developer, or sets a package-command in an ss-workflow workspace, even if they do not name the script. Windows PowerShell 3.0 or later with .NET Framework 4.5 or later; works with any version source (a file, git tags, or none).'
---

# Release package script

A release builds the tagged version and uploads its zip. This skill puts a small
PowerShell script into the project that makes that zip from a list of files that is
versioned with the code, so that the developer, a CI job, and a release produce the same
file, with no workspace and no plugin installed.

How the script behaves (parameters, version rules, what it refuses):
[references/package-script.md](references/package-script.md). Read it before you write
`package.psd1`.

## When the script is the right way

| Way | Who decides what goes into the zip |
|-----|------------------------------------|
| This script, in the project | `tools/package.psd1` in the project repository |
| The agent, from a list of glob patterns in the workspace settings (`package-files`) | The agent, at each release |

The script is the better way for a Windows project that ships binaries: the zip is the
same whoever makes it, and the list of files is reviewed with the code that produces
them. It does not depend on where the version comes from. For a project that ships
something else (an installer built by another tool, a web publish), keep its own
`package-command` and do not add the script.

## The change to make

1. Copy `New-ReleasePackage.ps1` from this skill's `scripts/` folder into the project
   repository's `tools/` folder, unchanged. It is copied, not referenced, so that the
   project packages without this plugin. It carries its version in a line
   `ss-build-script: New-ReleasePackage <version>`, which the update check below
   compares. If `Update-GitVersion.ps1` (from the `git-version` skill) is next to it, the
   script uses its version when no tag is on `HEAD`.
2. Write `tools/package.psd1`. This file belongs to the project; an update never
   overwrites it. Before writing it, propose `Platforms` and `Files` to the developer
   and use their answer:
   - **Platforms**: read the solution file, section `SolutionConfigurationPlatforms`,
     and list the platforms of its `Release|<platform>` entries. Ask which of them ship.
     One platform, or `Any CPU` alone, means no `Platforms`.
   - **Files**: the executables and libraries of the release output (`OutputPath`,
     `OutDir` of the project files), and anything else the developer names
     (configuration files, documents).

   ```powershell
   @{
       Name      = 'SSDisp'                  # the zip is SSDisp-v<version>.zip
       Platforms = @('x86', 'x64')           # optional: one zip per platform
       Files     = @(
           @{ From = 'SSDisp\bin\{Platform}\Release\*.exe'; To = '' }
           @{ From = 'SSDisp\bin\{Platform}\Release\*.dll'; To = '' }
           @{ From = 'config';                             To = 'config' }
       )
   }
   ```
3. Add `dist/` to the project's `.gitignore` (the script writes there by default).
4. Set the project's package command to
   `powershell -NoProfile -ExecutionPolicy Bypass -File tools\New-ReleasePackage.ps1`,
   wherever the project keeps it (in a workspace: `package-command` of its entry, with
   `package-files` empty).
5. **Verify** with a real run: build the release of every listed platform, then run the
   script with `-Version 0.0.0` on a clean checkout. It must write one zip per platform
   into `dist/` with the expected entries, and `git status` must stay clean. A pattern
   that matches nothing fails the run, which means the build did not make that file.

## In a workspace

The project is a submodule: start a `chore` request with `ss-workflow:new-req`, one per
project, with the root checkout on `develop` (if you cannot invoke it, tell the developer
to run `/ss-workflow:new-req` with the text). Give it this text, in the workspace's
requests language, with `Platforms` and `Files` already agreed with the developer:

```
Type: chore. Project: <name>.
Make the release package of <name> with the package script of ss-build
<plugin version>.

- Copy New-ReleasePackage.ps1 from the ss-build plugin (skills/release-package/scripts/)
  into <name>/tools/, unchanged.
- Write <name>/tools/package.psd1: Name = '<name>', Platforms = <the platforms that
  ship, or none>, Files = <the files, from the Release output>.
- Add dist/ to the project's .gitignore.
- Workspace: set package-command of <name> to
  "powershell -NoProfile -ExecutionPolicy Bypass -File tools\New-ReleasePackage.ps1",
  empty its package-files, and describe the script under "Toolchain" in its section of
  projects/AGENTS.md (tools/package.psd1 lists the files; a new output file or platform
  is added there; Windows PowerShell 3.0 or later needed).

Verify: the release build of every listed platform, then the script with -Version 0.0.0
on a clean checkout, writes one zip per platform into dist/ with the expected files; git
status of the project stays clean.
```

The request changes `package-command` and `package-files` on its own branch, so the new
values reach `develop` with the merge. Until then a release packages the project the old
way.

## Updating the copied script

Same check as the `git-version` skill: find `New-ReleasePackage.ps1` on the project's
develop branch, compare its `ss-build-script:` line (older copies carry
`ss-workflow-script:`, version 1.0.0, same content) with the file in `scripts/`, report,
and change nothing without the developer's go-ahead. An update copies the script
unchanged and never touches `package.psd1`.
