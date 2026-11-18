<#
.SYNOPSIS
  Build and run the host-side unit tests with gcc, clang, or MSVC (Windows PowerShell 5.1+).

.DESCRIPTION
  Compiles the layers under test together with the mocks and the test harness for the PC,
  not for the MCU, and runs the result. No hardware and no vendor SDK are needed, because
  the code under test depends on interfaces only. The first compiler found is used, in
  this order: gcc, clang, cl (a Visual Studio install is located with vswhere when cl is
  not on PATH). The exit code is the number of failed steps (0 = all tests passed).
#>
[CmdletBinding()]
param(
    [string]$OutDir = (Join-Path ([System.IO.Path]::GetTempPath()) 'fw-host-test')
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot

# Add every folder that holds code under test; the include paths follow the layers.
$includeDirs = 'common', 'hal', 'drivers', 'services', 'tests' | ForEach-Object { Join-Path $root $_ }
$sources = @(
    'drivers\drv_tmp102.c',
    'services\svc_thermal_monitor.c',
    'tests\fw_test.c',
    'tests\mock_i2c.c',
    'tests\test_tmp102.c',
    'tests\test_thermal_monitor.c',
    'tests\test_main.c'
) | ForEach-Object { Join-Path $root $_ }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null
$exe = Join-Path $OutDir 'fw_tests.exe'
if (Test-Path $exe) { Remove-Item $exe -Force }

function Find-Tool([string]$name) {
    $cmd = Get-Command $name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    return $null
}

function Find-VcVars {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) { return $null }
    $install = & $vswhere -latest -products * -property installationPath
    if (-not $install) { return $null }
    $vcvars = Join-Path $install 'VC\Auxiliary\Build\vcvars64.bat'
    if (Test-Path $vcvars) { return $vcvars }
    return $null
}

$gcc = Find-Tool 'gcc'
$clang = Find-Tool 'clang'
$cl = Find-Tool 'cl'

if ($gcc -or $clang) {
    $compiler = if ($gcc) { $gcc } else { $clang }
    $args = @('-std=c99', '-Wall', '-Wextra', '-Wpedantic', '-Werror') +
            ($includeDirs | ForEach-Object { "-I$_" }) + $sources + @('-o', $exe)
    Write-Host "Compiler: $compiler"
    & $compiler @args
    if ($LASTEXITCODE -ne 0) { exit 1 }
}
elseif ($cl -or (Find-VcVars)) {
    $includeArgs = ($includeDirs | ForEach-Object { '/I"{0}"' -f $_ }) -join ' '
    $sourceArgs = ($sources | ForEach-Object { '"{0}"' -f $_ }) -join ' '
    # A doubled trailing backslash keeps the closing quote of /Fo from being escaped.
    $outArgs = '/Fo"{0}\\" /Fe"{1}"' -f $OutDir.TrimEnd('\'), $exe
    $clArgs = "/nologo /std:c11 /W4 /WX $includeArgs $sourceArgs $outArgs"
    if ($cl) {
        Write-Host "Compiler: $cl"
        $line = "cl $clArgs"
    }
    else {
        $vcvars = Find-VcVars
        Write-Host "Compiler: cl (via $vcvars)"
        $line = 'call "{0}" >nul 2>&1 && cl {1}' -f $vcvars, $clArgs
    }
    & cmd.exe /c $line
    if ($LASTEXITCODE -ne 0) { exit 1 }
}
else {
    Write-Error 'No C compiler found. Install gcc or clang, or Visual Studio with the C++ workload.'
    exit 1
}

& $exe
exit $LASTEXITCODE
