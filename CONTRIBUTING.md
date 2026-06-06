# Contributing

Thanks for helping improve Audio Camera Master Widget.

## Development Setup

Prerequisites:

- Windows.
- .NET 10 SDK, as selected by `global.json`.
- .NET 8 Windows Desktop Runtime for framework-dependent local runs.
- NSIS only when building the installer locally.

Restore, build, and test:

```powershell
dotnet restore AudioCameraControlPanel.sln
dotnet build AudioCameraControlPanel.sln --no-restore
dotnet test AudioCameraControlPanel.sln --no-build
```

Run from source:

```powershell
dotnet run --project AudioCameraControlPanel\AudioCameraControlPanel.csproj
```

## Pull Requests

- Keep changes focused.
- Add or update tests for behavior changes.
- Run build and tests before opening a pull request.
- Update README or other docs when behavior, installation, or release steps change.

## Code Style

The project uses nullable reference types, implicit usings, and .NET analyzers through `Directory.Build.props`.

Before submitting:

```powershell
dotnet format AudioCameraControlPanel.sln --verify-no-changes --no-restore
```

## Releases

Maintainers create releases by pushing SemVer tags:

```powershell
git tag v1.0.1
git push origin v1.0.1
```

The release workflow builds and publishes the installer and portable zip.
