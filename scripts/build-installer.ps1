param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

if ($Version -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$') {
    throw "Version must be SemVer without a leading v, for example 1.0.0 or 1.0.0-beta.1."
}

$numericVersion = ($Version -split "-", 2)[0]
$versionParts = $numericVersion.Split(".")
$productVersion = "$($versionParts[0]).$($versionParts[1]).$($versionParts[2]).0"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "AudioCameraControlPanel\AudioCameraControlPanel.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish-$Runtime"
$installerScript = Join-Path $repoRoot "installer\AudioCameraMasterWidget.nsi"
$localMakensis = Join-Path $repoRoot "tools\nsis-msys2\mingw32\bin\makensis.exe"
$makensisCandidates = @(
    $localMakensis,
    (Join-Path ${env:ProgramFiles(x86)} "NSIS\makensis.exe"),
    (Join-Path $env:ProgramFiles "NSIS\makensis.exe")
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

dotnet publish $projectPath `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    /p:Version=$Version `
    /p:AssemblyVersion=$productVersion `
    /p:FileVersion=$productVersion `
    /p:InformationalVersion=$Version `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:DebugType=none `
    /p:DebugSymbols=false `
    -o $publishDir

Get-ChildItem -LiteralPath $publishDir -Recurse -Filter "*.pdb" | Remove-Item -Force

$makensisPath = $makensisCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($makensisPath)) {
    $makensis = Get-Command makensis -ErrorAction SilentlyContinue
    if ($null -eq $makensis) {
        throw "NSIS makensis.exe was not found. Install NSIS, then rerun this script."
    }

    $makensisPath = $makensis.Source
}

& $makensisPath "/DAPP_VERSION=$Version" "/DAPP_PRODUCT_VERSION=$productVersion" $installerScript
