---
name: git-version
description: 'Take the version of a C# or C++ project built with MSBuild from its git tags instead of from files - set up the version generator (Update-GitVersion.ps1 and GitVersion.targets) in a project, convert a project that keeps its version in AssemblyInfo.cs, csproj properties, or version defines to it, add the import to a new project file, or update the copied scripts to the current version. Use this whenever the user wants the version number to come from git tags, asks for automatic or tag-based versioning, says that builds show the wrong or a stale version, wants the assembly or file version, the .rc version, or a version header generated at build time, or sets version-source to git-tag in an ss-workflow workspace, even if they do not name the scripts. Windows and MSBuild 4.0 or later only; not for CMake, Keil, ESP-IDF, Python, or Node projects.'
---

# Version from git tags

The version of a project is the highest version tag (`vX.Y.Z`, `vX.Y.Z-beta4`, ...)
that `HEAD` contains. The build reads it and writes it into a generated file that git
ignores, so no commit ever changes a version, and a release is just a tag. This skill
puts the generator into a project and removes the old places that carried the version.

How the generator behaves (the version it writes in each state, the files it writes,
the tags that count): [references/generator.md](references/generator.md). Read it
before you convert a project, because the conversion has to leave nothing that still
sets a version by hand.

## Before you start

1. **Check the toolchain.** The generator supports C# and C++ built with MSBuild 4.0 or
   later (Visual Studio 2010 or later), on Windows with Windows PowerShell 3.0 or
   later. For any other toolchain, say that it is not supported here: a project may
   still take its version from tags in its own way, and then there is nothing to
   copy. Do not improvise a generator for it.
2. **Find out where the version lives today**: `AssemblyInfo.cs` attributes, `<Version>`
   and friends in project files, version defines in headers, `.rc` files, installer
   scripts. Every one of them is removed or changed in the conversion; a second source
   of the version is the bug this skill exists to prevent.
3. **Workspace or plain repository?** If the folder (or a parent) has a root `AGENTS.md`
   with a "Workflow settings" section, it is an ss-workflow workspace and every change
   to a project goes through a request: use "In a workspace" below. Otherwise edit the
   repository directly.

## The change to make

1. Copy `Update-GitVersion.ps1` and `GitVersion.targets` from this skill's `scripts/`
   folder into the project repository's `tools/` folder, unchanged. They are copied, not
   referenced, so that the project builds without this plugin, on a build server, and
   from an old checkout. Each carries its version in a line `ss-build-script: <name>
   <version>`, which the update check below compares.
2. Import `GitVersion.targets` in **each project file that builds a binary**, with the
   path from that file (for example `<Import Project="..\tools\GitVersion.targets" />`):

   | Project file | Position |
   |--------------|----------|
   | Old-style `.csproj` | After `<Import Project="$(MSBuildToolsPath)\Microsoft.CSharp.targets" />` |
   | SDK-style `.csproj` | Anywhere inside `<Project>`, usually at the end |
   | `.vcxproj` | After `<Import Project="$(VCTargetsPath)\Microsoft.Cpp.targets" />`; a `GitVersionHeader` or `GitVersionPrefix` property goes before the import |

   Not `Directory.Build.targets`: Visual Studio 2015 and older (MSBuild 14 and older)
   do not read it, and an explicit import works in every version.
3. **C#**: remove `AssemblyVersion`, `AssemblyFileVersion`, and
   `AssemblyInformationalVersion` from `Properties/AssemblyInfo.cs`, and `<Version>`,
   `<VersionPrefix>`, `<AssemblyVersion>`, `<FileVersion>`, and `<InformationalVersion>`
   from the project files. Other attributes stay. The targets file adds the generated
   `obj\...\GitVersion.g.cs` to the compilation and turns off the SDK's own version
   attributes.
4. **C++**: include `GitVersion.g.h` where the version is used, also in the `.rc` file
   (`FILEVERSION` from the `VERSION_MAJOR/MINOR/PATCH/BUILD` macros, the version
   strings from `VERSION_STRING`), remove the old version defines, and add
   `GitVersion.g.h` to the project's `.gitignore`.
5. Tell the developer that a project file added later needs the same `<Import>`, and
   where the generator is (record it wherever the project documents its toolchain).
6. **Verify**: the build passes; `git status` is clean after the build (the generated
   file is ignored); on a commit with a tag `vX.Y.Z` the built binaries carry `X.Y.Z`,
   and one commit after it `X.Y.Z-1-g<sha>`. Run the build, do not claim it from
   reading the files.

## In a workspace

The project is a submodule: the change is a project change, and the workspace records
that the project now takes its version from tags.

- Start a `chore` request with `ss-workflow:new-req`, one per project, with the root
  checkout on `develop`. Give it the text below as its input (in the workspace's
  requests language); new-req drafts the spec from it and discusses it as usual. If you
  cannot invoke it, tell the developer to run `/ss-workflow:new-req` with that text.

  ```
  Type: chore. Project: <name>.
  Take the version of <name> from its git tags (version-source: git-tag), with the
  version generator of ss-build <plugin version>.

  <the change above, filled in for this project: the files to copy and where, the
  project files that import it, the old version places to remove>

  Workspace: set version-source of <name> to git-tag in the workspace settings, and
  describe the generator under "Toolchain" in its section of projects/AGENTS.md (where
  the files are, that a new project file needs the same <Import>, Windows PowerShell
  3.0 or later needed). Remove the other places that carried the version from that
  section.

  Verify: the build passes; git status of the project is clean after the build; on a
  commit with a tag vX.Y.Z the built binaries carry X.Y.Z, and one commit after it
  X.Y.Z-1-g<sha>.
  ```

- Until that request is merged, a release of the project still sets the version in its
  file: the request changes `version-source` on its own branch, so the new value reaches
  `develop` together with the project change it describes.

## Updating the copied scripts

Run this when the developer asks, when a release or upgrade reports an older script, or
before you change a project that already uses the generator.

1. Find the files on the project's develop branch: `git -C <path> ls-tree -r --name-only
   <develop-ref>`, matching `Update-GitVersion.ps1` and `GitVersion.targets`.
2. Read the version in the `ss-build-script:` line of each (copies made before this
   plugin carry `ss-workflow-script:` and are version 1.0.0, the same content), and
   compare it with the file of the same name in `scripts/`.
3. Report, and change nothing without the developer's go-ahead:
   - An older version: offer a `chore` change (in a workspace, a request "update the
     version generator of <name> to <version>") that copies the new files unchanged.
   - No file found where the settings expect it: the change that sets it up has not been
     merged, or the project does it in its own way. Say so.
   - A file that differs from the same version here: someone changed it in the project.
     Report it; an update would overwrite that change.
