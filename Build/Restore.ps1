# Restores all packages required to build the Terminals solution.
#
# After the SDK-style project migration, the regular solution restore covers
# PackageReference entries, but two packages cannot be resolved that way and
# are referenced by HintPath into Source\packages:
#   - EntityFramework 5.0.0 must use the net40 lib (assembly 4.4.0.0); the
#     PackageReference restore would select the net45 lib (assembly 5.0.0.0)
#     which is incompatible with the application configuration and edmx.
#   - Microsoft.VisualStudio.QualityTools.UnitTestFramework.Updated has a
#     malformed package layout (assembly group instead of a target framework)
#     and cannot be restored through PackageReference at all (NU1202).
#
# Usage: run once after cloning (or after changing package versions):
#   powershell -ExecutionPolicy Bypass -File Build\Restore.ps1
param(
    [string]$PackagesDirectory = "Source\packages"
)

$repoRoot = Split-Path -Parent $PSScriptRoot
$packagesDir = Join-Path $repoRoot $PackagesDirectory
$solution = Join-Path $repoRoot "Source\Terminals.sln"

Write-Host "Installing HintPath-based packages into $PackagesDirectory ..."
nuget install Microsoft.VisualStudio.QualityTools.UnitTestFramework.Updated -Version 15.0.26228 -OutputDirectory $PackagesDirectory -ExcludeVersion
if ($LASTEXITCODE -ne 0) { throw "nuget install failed for Microsoft.VisualStudio.QualityTools.UnitTestFramework.Updated" }

nuget install EntityFramework -Version 5.0.0 -OutputDirectory $PackagesDirectory -ExcludeVersion
if ($LASTEXITCODE -ne 0) { throw "nuget install failed for EntityFramework" }

Write-Host "Restoring solution packages (PackageReference) ..."
msbuild $solution -t:restore -property:Configuration=Release
if ($LASTEXITCODE -ne 0) { throw "msbuild restore failed" }

Write-Host "Restore completed. Build with: msbuild $solution -t:rebuild -property:Configuration=Release -restore"
