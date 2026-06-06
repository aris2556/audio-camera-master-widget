# Audio Camera Master Widget

A Windows 11 WPF desktop app that opens the compact master widget directly.

## Stack

- C# / .NET 8
- WPF
- Core Audio MMDevice / WASAPI endpoint APIs for render and capture devices
- MSTest for non-hardware behavior tests

No admin rights are required.

## Features

- Starts directly in the master widget instead of the full control panel.
- Opens the full control panel on demand from the master widget while keeping widget state shared.
- Lists output/render audio devices and the selected microphone/input device.
- Reads and sets endpoint volume through `IAudioEndpointVolume` when available.
- Provides quick output and input volume presets: 0%, 30%, 45%, and 60%.
- Reads and sets endpoint mute through `IAudioEndpointVolume` when available.
- Shows a live microphone/input peak meter when available.
- Opens native Windows settings pages for sound, app volume, camera, camera privacy, and microphone privacy.
- Persists the last selected output, input, and camera IDs under `%LOCALAPPDATA%\AudioCameraControlPanel\settings.json`.

## Build

From this directory in Windows PowerShell:

```powershell
dotnet restore AudioCameraControlPanel.sln
dotnet build AudioCameraControlPanel.sln --no-restore
dotnet test AudioCameraControlPanel.sln --no-build
```

SDK selection is pinned by `global.json` to the .NET 10 SDK feature band. The app targets `net8.0-windows`, so install the .NET 8 Desktop Runtime on machines that run the app.

## Run

From Windows PowerShell:

```powershell
dotnet run --project AudioCameraControlPanel\AudioCameraControlPanel.csproj
```

## Publish

Framework-dependent Windows x64 single-file desktop publish:

```powershell
dotnet publish AudioCameraControlPanel\AudioCameraControlPanel.csproj -c Release -r win-x64 --self-contained false /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

The published app is written to:

```text
AudioCameraControlPanel/bin/Release/net8.0-windows10.0.19041.0/win-x64/publish/
```

Run `AudioCameraMasterWidget.exe` from that folder.

The default publish uses a single-file executable because some Windows Application Control policies block loading unsigned loose app DLLs from local publish folders.

## Installer

The NSIS installer installs per-user to:

```text
%LOCALAPPDATA%\Programs\AudioCameraMasterWidget
```

Build the installer from Windows PowerShell:

```powershell
.\scripts\build-installer.ps1
```

The installer is written to:

```text
artifacts\AudioCameraMasterWidgetSetup.exe
```

If `makensis.exe` is not installed globally, the build script also checks `tools\nsis-msys2\mingw32\bin\makensis.exe`.

The installer is unsigned. If Windows Application Control or Smart App Control blocks unsigned local apps, the durable fix is to sign the installer and app executable with a trusted code-signing certificate or relax that policy for this app.

### WSL or non-Windows cross-targeting

Windows PowerShell on Windows is the primary path for build, run, test, and publish. From WSL or another non-Windows environment, restore/build/test/publish only as cross-targeting operations and add `/p:EnableWindowsTargeting=true` to the commands above. Running the WPF app still requires Windows.

## Limitations

- Setting the Windows default audio device is not implemented. Windows does not expose a stable public desktop API for changing the default endpoint, so the widget provides Windows Sound Settings shortcuts instead.
- Compact widgets are regular WPF desktop windows, not integrations with the Windows Widgets board.
- The master widget intentionally omits camera preview and does not start camera capture; it only links to camera settings/privacy.
- Some microphones or audio endpoints may not expose endpoint volume, mute, or metering controls. The UI disables unsupported controls and shows a status message.
- If the app does not start on another PC, install the .NET 8 Desktop Runtime or publish self-contained with `--self-contained true`.
