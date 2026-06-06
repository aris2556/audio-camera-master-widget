# App Improvements Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Implement the top ten improvements for the Audio Camera Master Widget while preserving the current compact-widget-first behavior.

**Architecture:** Introduce focused view models for output audio, input audio, camera preview, and shell/window orchestration, with `MainViewModel` kept as a compatibility facade for existing bindings. Move repeated WPF UI sections into reusable `UserControl`s so full and compact windows share behavior and layout. Harden services around event notifications, atomic settings persistence, command error handling, and project hygiene.

**Tech Stack:** C#/.NET 8, WPF, MSTest, Windows Core Audio MMDevice/EndpointVolume COM APIs, WinRT `MediaCapture`/`MediaFrameReader`, GitHub Actions on Windows.

---

## Research Notes

- Microsoft documents `IMMNotificationClient` for endpoint add/remove/state/default-device notifications and warns callbacks must be nonblocking.
- Microsoft documents `IAudioEndpointVolumeCallback` for endpoint volume/mute changes and also warns callbacks must be nonblocking.
- `MediaFrameReader.FrameArrived` is raised off the UI thread; frame handlers should acquire latest frames, dispose bitmap resources, and marshal UI updates through the dispatcher.
- WPF UI objects belong to the dispatcher thread that created them; use dispatcher async posting for background callbacks.
- `File.Replace` can replace an existing settings file from a temp file and can create a backup.
- `global.json` pins SDK selection independently from the target framework and is appropriate for CI consistency.

## Task 1: Test Harness And Fakes

**Files:**
- Create: `AudioCameraControlPanel.Tests/Fakes/FakeAudioDeviceService.cs`
- Create: `AudioCameraControlPanel.Tests/Fakes/FakeCameraDeviceService.cs`
- Create: `AudioCameraControlPanel.Tests/Fakes/FakeSettingsLauncherService.cs`
- Create: `AudioCameraControlPanel.Tests/Fakes/InMemoryAppSettingsStore.cs`
- Modify: `AudioCameraControlPanel.Tests/AudioCameraControlPanel.Tests.csproj`

**Steps:**
1. Add fake implementations for `IAudioDeviceService`, `ICameraDeviceService`, `ISettingsLauncherService`, and `IAppSettingsStore`.
2. Fakes must support device lists, endpoint state, failures, save counts, launched pages, and camera preview state.
3. Run `dotnet test AudioCameraControlPanel.sln --no-restore`.
4. Expected: existing 11 tests still pass.

## Task 2: Main View Model Behavior Tests

**Files:**
- Create: `AudioCameraControlPanel.Tests/MainViewModelTests.cs`
- Modify only test fakes if needed.

**Red tests:**
- `InitializeAsyncRestoresSavedOutputInputAndCameraSelections`
- `InitializeAsyncFallsBackToDefaultAudioDevicesWhenSavedIdsAreMissing`
- `OutputVolumePresetWritesEndpointVolumeWhenSupported`
- `MuteAllOnlyTouchesEndpointsThatSupportMute`
- `SaveFailureUpdatesStatusWithoutThrowing`
- `CameraPreviewErrorStopsPreviewAndClearsFrame`

**Commands:**
- Red: `dotnet test AudioCameraControlPanel.Tests/AudioCameraControlPanel.Tests.csproj --filter MainViewModelTests --no-restore`
- Green later: same command.

## Task 3: Split MainViewModel Into Focused Modules

**Files:**
- Create: `AudioCameraControlPanel/ViewModels/AudioEndpointViewModel.cs`
- Create: `AudioCameraControlPanel/ViewModels/CameraPreviewViewModel.cs`
- Create: `AudioCameraControlPanel/ViewModels/SettingsShortcutsViewModel.cs`
- Modify: `AudioCameraControlPanel/ViewModels/MainViewModel.cs`
- Modify: `AudioCameraControlPanel/Services/IAudioDeviceService.cs`
- Modify tests from Task 2 as needed.

**Acceptance:**
- `MainViewModel` remains compatible with current XAML property/command names.
- Output/input state logic moves into reusable endpoint view-model instances.
- Camera preview state moves into `CameraPreviewViewModel`.
- Windows settings launch logic moves into `SettingsShortcutsViewModel`.
- Existing XAML should not need broad binding changes yet.

**Verification:**
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 4: Make The Full Control Panel Explicit

**Files:**
- Modify: `AudioCameraControlPanel/App.xaml.cs`
- Modify: `AudioCameraControlPanel/MainWindow.xaml.cs`
- Modify: `AudioCameraControlPanel/ViewModels/MainViewModel.cs`
- Modify: `AudioCameraControlPanel/CompactWidgetWindow.xaml`
- Modify: `README.md`
- Test: `AudioCameraControlPanel.Tests/MainViewModelTests.cs`

**Acceptance:**
- Default startup still opens the master widget.
- A new command on the master widget opens the full control panel using the same `MainViewModel`.
- `MainWindow` can either own its own view model or use an injected existing one.
- Closing the full panel does not shut down the master widget when it is opened from the master widget.

**Verification:**
- Add a test that `OpenControlPanelCommand` raises an `OpenControlPanelRequested` event.
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 5: Extract Shared WPF Controls

**Files:**
- Create: `AudioCameraControlPanel/Controls/IconTextContent.xaml`
- Create: `AudioCameraControlPanel/Controls/IconTextContent.xaml.cs`
- Create: `AudioCameraControlPanel/Controls/VolumePresetStrip.xaml`
- Create: `AudioCameraControlPanel/Controls/VolumePresetStrip.xaml.cs`
- Create: `AudioCameraControlPanel/Controls/CameraPreviewPane.xaml`
- Create: `AudioCameraControlPanel/Controls/CameraPreviewPane.xaml.cs`
- Create: `AudioCameraControlPanel/Controls/OutputAudioControl.xaml`
- Create: `AudioCameraControlPanel/Controls/OutputAudioControl.xaml.cs`
- Create: `AudioCameraControlPanel/Controls/InputAudioControl.xaml`
- Create: `AudioCameraControlPanel/Controls/InputAudioControl.xaml.cs`
- Create: `AudioCameraControlPanel/Controls/CameraPreviewControl.xaml`
- Create: `AudioCameraControlPanel/Controls/CameraPreviewControl.xaml.cs`
- Create: `AudioCameraControlPanel/Controls/SettingsShortcutsControl.xaml`
- Create: `AudioCameraControlPanel/Controls/SettingsShortcutsControl.xaml.cs`
- Modify: `AudioCameraControlPanel/MainWindow.xaml`
- Modify: `AudioCameraControlPanel/CompactWidgetWindow.xaml`

**Acceptance:**
- Start with low-risk controls: icon/text button content, preset strips, and camera preview pane.
- Full and compact windows reuse the controls for output, input, camera, and settings.
- Compact-specific layout stays compact through control properties or simple wrapper layout.
- Bindings still work through the existing `MainViewModel` facade.
- Controls must not set their own `DataContext`; internal bindings use `RelativeSource AncestorType=UserControl`.

**Verification:**
- Run `dotnet build AudioCameraControlPanel.sln --no-restore`.
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 6: Atomic And Debounced Settings Persistence

**Files:**
- Modify: `AudioCameraControlPanel/Services/JsonAppSettingsStore.cs`
- Modify: `AudioCameraControlPanel/ViewModels/MainViewModel.cs`
- Modify: `AudioCameraControlPanel.Tests/JsonAppSettingsStoreTests.cs`
- Modify: `AudioCameraControlPanel.Tests/MainViewModelTests.cs`

**Red tests:**
- Saving existing settings leaves valid JSON after overwrite.
- Rapid output/input/camera selection changes collapse into fewer saves.
- Dispose flushes pending settings before app exit.

**Acceptance:**
- Write settings to a temp file in the settings directory first.
- Replace existing settings with `File.Replace` when present; otherwise move the temp file into place.
- Keep the `_saveLock`.
- Debounce view-model save requests with a short delay and flush on dispose.

**Verification:**
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 7: Audio Device And Endpoint Notifications

**Files:**
- Modify: `AudioCameraControlPanel/Services/CoreAudioInterop.cs`
- Modify: `AudioCameraControlPanel/Services/IAudioDeviceService.cs`
- Modify: `AudioCameraControlPanel/Services/AudioDeviceService.cs`
- Modify: `AudioCameraControlPanel/ViewModels/MainViewModel.cs`
- Modify: `AudioCameraControlPanel.Tests/CoreAudioInteropContractTests.cs`
- Modify: `AudioCameraControlPanel.Tests/MainViewModelTests.cs`

**Red tests:**
- Service interface exposes device change and endpoint state change events.
- View model refreshes output/input lists on device/default changes.
- View model refreshes endpoint state on selected endpoint volume/mute notification.

**Acceptance:**
- Implement `IMMNotificationClient` and `IAudioEndpointVolumeCallback` interop contracts.
- Callbacks must only enqueue/raise managed events and must not block.
- Register notifications while `AudioDeviceService` is alive and unregister on dispose.
- `MainViewModel` subscribes and marshals refresh/state updates to the WPF dispatcher.
- Preserve selected device IDs where possible when add/remove/default notifications refresh lists.

**Verification:**
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 8: Command Error Handling And Slider Throttling

**Files:**
- Modify: `AudioCameraControlPanel/ViewModels/AsyncRelayCommand.cs`
- Modify: `AudioCameraControlPanel/ViewModels/MainViewModel.cs`
- Modify: `AudioCameraControlPanel.Tests/MainViewModelTests.cs`
- Create: `AudioCameraControlPanel.Tests/AsyncRelayCommandTests.cs`

**Red tests:**
- `AsyncRelayCommand` reports execution errors through a callback instead of leaking exceptions.
- Volume changes are debounced before calling `TrySetVolume`.
- Final slider value wins during rapid changes.

**Acceptance:**
- Add optional async-command error callback.
- Expose command `IsExecuting` if useful to bindings.
- Debounce volume writes separately for output and input.
- Avoid activating the input meter COM object on every 200ms tick when the selected input device has not changed.

**Verification:**
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 9: Camera Preview Lifecycle And Frame Throttling

**Files:**
- Modify: `AudioCameraControlPanel/Services/CameraDeviceService.cs`
- Modify: `AudioCameraControlPanel/ViewModels/CameraPreviewViewModel.cs`
- Modify: `AudioCameraControlPanel/ViewModels/MainViewModel.cs`
- Modify: `AudioCameraControlPanel.Tests/MainViewModelTests.cs`

**Red tests:**
- Preview error stops preview, clears frame, and updates status.
- Selecting a new camera while preview is running restarts preview once.

**Acceptance:**
- Limit frame conversion/UI publication to a reasonable preview FPS.
- Continue disposing acquired frames promptly.
- Ensure preview errors make view-model state reflect stopped preview.

**Verification:**
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Task 10: Project Hygiene, CI, And Documentation

**Files:**
- Create: `global.json`
- Create: `Directory.Build.props`
- Create: `.github/workflows/windows-ci.yml`
- Modify: `README.md`
- Optionally remove generated `bin/`, `obj/`, and `TestResults/` directories after verifying paths are inside the workspace.

**Acceptance:**
- CI uses `windows-latest`, restores, builds, and tests the solution.
- `global.json` pins an SDK compatible with installed Windows SDK behavior.
- `Directory.Build.props` centralizes nullable/implicit usings/analyzer settings without breaking the build.
- README uses Windows PowerShell commands first for this Windows app.

**Verification:**
- Run `dotnet build AudioCameraControlPanel.sln --no-restore`.
- Run `dotnet test AudioCameraControlPanel.sln --no-restore`.

## Final Review

After all tasks:
- Run full tests.
- Run a subagent spec review against this plan.
- Run a subagent code-quality review.
- Manually inspect generated file cleanup and ensure no project files were removed.
