# Audio Camera Master Widget

Windows WPF app for local audio endpoint controls, microphone metering, camera preview, and Windows settings shortcuts.

The app starts with the compact master widget. The full control panel can be opened from that widget.

## Requirements

- Windows.
- .NET 10 SDK to build this repo, as selected by `global.json`.
- .NET 8 Windows Desktop Runtime to run framework-dependent builds.
- NSIS `makensis.exe` only when building the installer.

The app targets `net8.0-windows10.0.19041.0`.

## Build And Test

Run from this directory in Windows PowerShell:

```powershell
dotnet restore AudioCameraControlPanel.sln
dotnet build AudioCameraControlPanel.sln --no-restore
dotnet test AudioCameraControlPanel.sln --no-build
```

Generated outputs are ignored by git:

```text
bin/
obj/
artifacts/
tools/
TestResults/
```

## Run From Source

```powershell
dotnet run --project AudioCameraControlPanel\AudioCameraControlPanel.csproj
```

## Publish

Framework-dependent single-file publish:

```powershell
dotnet publish AudioCameraControlPanel\AudioCameraControlPanel.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Self-contained single-file publish:

```powershell
dotnet publish AudioCameraControlPanel\AudioCameraControlPanel.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true -o artifacts\publish-win-x64
```

The NSIS installer script uses the self-contained publish output in:

```text
artifacts\publish-win-x64
```

## Installer

Build the installer:

```powershell
.\scripts\build-installer.ps1
```

The installer is written to:

```text
artifacts\AudioCameraMasterWidgetSetup.exe
```

The installer installs per-user to:

```text
%LOCALAPPDATA%\Programs\AudioCameraMasterWidget
```

The Start Menu folder is:

```text
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Audio Camera Master Widget
```

The desktop shortcut is:

```text
%USERPROFILE%\Desktop\Audio Camera Master Widget.lnk
```

`scripts\build-installer.ps1` looks for `makensis.exe` in:

```text
tools\nsis-msys2\mingw32\bin\makensis.exe
```

If that file is not present, it uses `makensis.exe` from `PATH`.

## Features

- Lists active output and input audio devices.
- Shows the default output and input endpoint reported by Windows.
- Reads endpoint volume and mute state when available.
- Sets endpoint volume and mute state when available.
- Provides 0%, 30%, 45%, and 60% volume preset buttons.
- Shows a live microphone peak meter when available.
- Lists local cameras.
- Starts and stops local camera preview.
- Opens Windows settings pages for sound, app volume, camera, camera privacy, and microphone privacy.
- Stores the last selected output, input, and camera IDs.

## Stored Data

Settings are stored at:

```text
%LOCALAPPDATA%\AudioCameraControlPanel\settings.json
```

The settings file stores device IDs only. It does not store audio, video, or images.

## Implementation Notes

- Audio device enumeration uses Core Audio MMDevice APIs.
- Endpoint volume and mute use `IAudioEndpointVolume`.
- Audio device/default changes use `IMMNotificationClient`.
- Endpoint volume/mute changes use `IAudioEndpointVolumeCallback`.
- Camera enumeration and preview use WinRT media capture APIs.
- Tests use MSTest and fake hardware services.
- CI runs on `windows-latest`.

## Code Signing

The app and installer are unsigned.

Windows Application Control, Smart App Control, or enterprise code integrity policy can block unsigned executables or DLLs. The repo does not include a signing certificate.

## Limitations

- The app does not change the Windows default audio device.
- Some devices do not expose endpoint volume, mute, or metering controls.
- Compact widgets are normal WPF windows, not Windows Widgets board integrations.
- Camera preview is local only. The app does not record or save camera frames.
- Building or running the WPF app requires Windows. Non-Windows environments can only cross-target with `/p:EnableWindowsTargeting=true`.
