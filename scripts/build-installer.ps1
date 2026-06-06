param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "AudioCameraControlPanel\AudioCameraControlPanel.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish-$Runtime"
$installerScript = Join-Path $repoRoot "installer\AudioCameraMasterWidget.nsi"
$localMakensis = Join-Path $repoRoot "tools\nsis-msys2\mingw32\bin\makensis.exe"

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $publishDir

$makensisPath = $localMakensis
if (-not (Test-Path -LiteralPath $makensisPath)) {
    $makensis = Get-Command makensis -ErrorAction SilentlyContinue
    if ($null -eq $makensis) {
        throw "NSIS makensis.exe was not found. Install NSIS, then rerun this script."
    }

    $makensisPath = $makensis.Source
}

& $makensisPath $installerScript
