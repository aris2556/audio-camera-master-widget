using System.Runtime.InteropServices;
using AudioCameraControlPanel.Models;

namespace AudioCameraControlPanel.Services;

public sealed class AudioDeviceService : IAudioDeviceService
{
    private readonly object _notificationSync = new();
    private readonly Dictionary<string, EndpointVolumeSubscription> _endpointSubscriptions =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IAudioMeterInformation> _meterCache =
        new(StringComparer.OrdinalIgnoreCase);
    private IMMDeviceEnumerator? _notificationEnumerator;
    private AudioNotificationClient? _notificationClient;
    private bool _disposed;

    public AudioDeviceService()
    {
        TryRegisterDeviceNotifications();
    }

    public event EventHandler<AudioDevicesChangedEventArgs>? DevicesChanged;

    public event EventHandler<AudioEndpointChangedEventArgs>? EndpointStateChanged;

    public IReadOnlyList<AudioDeviceInfo> GetOutputDevices()
    {
        return GetDevices(EDataFlow.Render, AudioDirection.Output);
    }

    public IReadOnlyList<AudioDeviceInfo> GetInputDevices()
    {
        return GetDevices(EDataFlow.Capture, AudioDirection.Input);
    }

    public string? GetDefaultOutputDeviceId()
    {
        return GetDefaultDeviceId(EDataFlow.Render);
    }

    public string? GetDefaultInputDeviceId()
    {
        return GetDefaultDeviceId(EDataFlow.Capture);
    }

    public AudioEndpointState GetEndpointState(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            return new AudioEndpointState(null, null, false, false, "No audio device is selected.");
        }

        IMMDevice? device = null;
        IAudioEndpointVolume? endpointVolume = null;

        try
        {
            device = GetDevice(deviceId);
            endpointVolume = Activate<IAudioEndpointVolume>(device);

            var canReadVolume = endpointVolume.GetMasterVolumeLevelScalar(out var volume) >= 0;
            var canReadMute = endpointVolume.GetMute(out var isMuted) >= 0;

            var endpointState = new AudioEndpointState(
                canReadVolume ? Math.Round(volume * 100, 0) : null,
                canReadMute ? isMuted : null,
                canReadVolume,
                canReadMute);
            EnsureEndpointVolumeSubscription(deviceId);
            return endpointState;
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
            return new AudioEndpointState(null, null, false, false, FormatAudioError("volume/mute control", ex));
        }
        finally
        {
            ReleaseCom(endpointVolume);
            ReleaseCom(device);
        }
    }

    public bool TrySetVolume(string deviceId, double volumePercent, out string? errorMessage)
    {
        errorMessage = null;
        IMMDevice? device = null;
        IAudioEndpointVolume? endpointVolume = null;

        try
        {
            device = GetDevice(deviceId);
            endpointVolume = Activate<IAudioEndpointVolume>(device);

            var scalar = (float)Math.Clamp(volumePercent / 100d, 0d, 1d);
            var hresult = endpointVolume.SetMasterVolumeLevelScalar(scalar, IntPtr.Zero);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            return true;
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
            errorMessage = FormatAudioError("set volume", ex);
            return false;
        }
        finally
        {
            ReleaseCom(endpointVolume);
            ReleaseCom(device);
        }
    }

    public bool TrySetMute(string deviceId, bool isMuted, out string? errorMessage)
    {
        errorMessage = null;
        IMMDevice? device = null;
        IAudioEndpointVolume? endpointVolume = null;

        try
        {
            device = GetDevice(deviceId);
            endpointVolume = Activate<IAudioEndpointVolume>(device);

            var hresult = endpointVolume.SetMute(isMuted, IntPtr.Zero);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            return true;
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
            errorMessage = FormatAudioError("set mute", ex);
            return false;
        }
        finally
        {
            ReleaseCom(endpointVolume);
            ReleaseCom(device);
        }
    }

    public double? GetPeakLevelPercent(string deviceId)
    {
        try
        {
            var meter = GetOrCreateMeter(deviceId);
            return meter.GetPeakValue(out var peak) >= 0
                ? Math.Round(Math.Clamp(peak, 0f, 1f) * 100, 0)
                : null;
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
            RemoveEndpointResources(deviceId);
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        EndpointVolumeSubscription[] subscriptions;
        IAudioMeterInformation[] meters;
        IMMDeviceEnumerator? notificationEnumerator;
        AudioNotificationClient? notificationClient;
        lock (_notificationSync)
        {
            subscriptions = _endpointSubscriptions.Values.ToArray();
            meters = _meterCache.Values.ToArray();
            _endpointSubscriptions.Clear();
            _meterCache.Clear();
            notificationEnumerator = _notificationEnumerator;
            notificationClient = _notificationClient;
            _notificationEnumerator = null;
            _notificationClient = null;
        }

        if (notificationEnumerator is not null && notificationClient is not null)
        {
            try
            {
                notificationEnumerator.UnregisterEndpointNotificationCallback(notificationClient);
            }
            catch (Exception ex) when (IsDeviceException(ex))
            {
            }
        }

        foreach (var subscription in subscriptions)
        {
            subscription.Dispose();
        }

        foreach (var meter in meters)
        {
            ReleaseCom(meter);
        }

        ReleaseCom(notificationEnumerator);
        GC.SuppressFinalize(this);
    }

    private void TryRegisterDeviceNotifications()
    {
        IMMDeviceEnumerator? enumerator = null;
        try
        {
            enumerator = CreateEnumerator();
            var client = new AudioNotificationClient(OnCoreAudioDevicesChanged);
            CoreAudioInterop.ThrowIfFailed(enumerator.RegisterEndpointNotificationCallback(client));

            lock (_notificationSync)
            {
                if (_disposed)
                {
                    enumerator.UnregisterEndpointNotificationCallback(client);
                    return;
                }

                _notificationEnumerator = enumerator;
                _notificationClient = client;
                enumerator = null;
            }
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
        }
        finally
        {
            ReleaseCom(enumerator);
        }
    }

    private void OnCoreAudioDevicesChanged(AudioDevicesChangedEventArgs args, DeviceState? newState)
    {
        if (args.DeviceId is not null &&
            newState is not null &&
            !newState.Value.HasFlag(DeviceState.Active))
        {
            RemoveEndpointResources(args.DeviceId);
        }

        DevicesChanged?.Invoke(this, args);
    }

    private void EnsureEndpointVolumeSubscription(string deviceId)
    {
        lock (_notificationSync)
        {
            if (_disposed || _endpointSubscriptions.ContainsKey(deviceId))
            {
                return;
            }
        }

        IMMDevice? device = null;
        IAudioEndpointVolume? endpointVolume = null;
        EndpointVolumeSubscription? subscription = null;
        try
        {
            device = GetDevice(deviceId);
            endpointVolume = Activate<IAudioEndpointVolume>(device);
            subscription = EndpointVolumeSubscription.Create(deviceId, endpointVolume, OnEndpointVolumeChanged);
            endpointVolume = null;

            lock (_notificationSync)
            {
                if (_disposed || _endpointSubscriptions.ContainsKey(deviceId))
                {
                    subscription.Dispose();
                    return;
                }

                _endpointSubscriptions[deviceId] = subscription;
                subscription = null;
            }
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
        }
        finally
        {
            subscription?.Dispose();
            ReleaseCom(endpointVolume);
            ReleaseCom(device);
        }
    }

    private void OnEndpointVolumeChanged(string deviceId)
    {
        EndpointStateChanged?.Invoke(this, new AudioEndpointChangedEventArgs(deviceId));
    }

    private IAudioMeterInformation GetOrCreateMeter(string deviceId)
    {
        lock (_notificationSync)
        {
            if (_meterCache.TryGetValue(deviceId, out var cachedMeter))
            {
                return cachedMeter;
            }
        }

        IMMDevice? device = null;
        IAudioMeterInformation? meter = null;
        try
        {
            device = GetDevice(deviceId);
            meter = Activate<IAudioMeterInformation>(device);

            lock (_notificationSync)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(AudioDeviceService));
                }

                if (_meterCache.TryGetValue(deviceId, out var cachedMeter))
                {
                    return cachedMeter;
                }

                _meterCache[deviceId] = meter;
                var retainedMeter = meter;
                meter = null;
                return retainedMeter;
            }
        }
        finally
        {
            ReleaseCom(meter);
            ReleaseCom(device);
        }
    }

    private void RemoveEndpointResources(string deviceId)
    {
        EndpointVolumeSubscription? subscription = null;
        IAudioMeterInformation? meter = null;

        lock (_notificationSync)
        {
            if (_endpointSubscriptions.Remove(deviceId, out var removedSubscription))
            {
                subscription = removedSubscription;
            }

            if (_meterCache.Remove(deviceId, out var removedMeter))
            {
                meter = removedMeter;
            }
        }

        subscription?.Dispose();
        ReleaseCom(meter);
    }

    private static IReadOnlyList<AudioDeviceInfo> GetDevices(EDataFlow flow, AudioDirection direction)
    {
        var devices = new List<AudioDeviceInfo>();
        var defaultDeviceId = GetDefaultDeviceId(flow);
        IMMDeviceEnumerator? enumerator = null;
        IMMDeviceCollection? collection = null;

        try
        {
            enumerator = CreateEnumerator();
            CoreAudioInterop.ThrowIfFailed(enumerator.EnumAudioEndpoints(flow, DeviceState.Active, out collection));
            CoreAudioInterop.ThrowIfFailed(collection.GetCount(out var count));

            for (uint index = 0; index < count; index++)
            {
                IMMDevice? device = null;
                try
                {
                    CoreAudioInterop.ThrowIfFailed(collection.Item(index, out device));
                    CoreAudioInterop.ThrowIfFailed(device.GetId(out var id));
                    devices.Add(new AudioDeviceInfo(
                        id,
                        GetFriendlyName(device) ?? id,
                        direction,
                        string.Equals(id, defaultDeviceId, StringComparison.OrdinalIgnoreCase)));
                }
                finally
                {
                    ReleaseCom(device);
                }
            }
        }
        finally
        {
            ReleaseCom(collection);
            ReleaseCom(enumerator);
        }

        return devices
            .OrderByDescending(device => device.IsDefault)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();
    }

    private static string? GetDefaultDeviceId(EDataFlow flow)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? endpoint = null;

        try
        {
            enumerator = CreateEnumerator();
            var hresult = enumerator.GetDefaultAudioEndpoint(flow, ERole.Multimedia, out endpoint);
            if (hresult < 0)
            {
                return null;
            }

            return endpoint.GetId(out var id) >= 0 ? id : null;
        }
        finally
        {
            ReleaseCom(endpoint);
            ReleaseCom(enumerator);
        }
    }

    private static IMMDevice GetDevice(string deviceId)
    {
        IMMDeviceEnumerator? enumerator = null;
        try
        {
            enumerator = CreateEnumerator();
            CoreAudioInterop.ThrowIfFailed(enumerator.GetDevice(deviceId, out var device));
            return device;
        }
        finally
        {
            ReleaseCom(enumerator);
        }
    }

    private static T Activate<T>(IMMDevice device)
        where T : class
    {
        var interfaceId = typeof(T).GUID;
        CoreAudioInterop.ThrowIfFailed(device.Activate(ref interfaceId, ClsCtx.InprocServer, IntPtr.Zero, out var instance));
        return (T)instance;
    }

    private static string? GetFriendlyName(IMMDevice device)
    {
        IPropertyStore? propertyStore = null;
        var propVariant = new PropVariant();

        try
        {
            CoreAudioInterop.ThrowIfFailed(device.OpenPropertyStore(StorageAccessMode.Read, out propertyStore));
            var propertyKey = CoreAudioInterop.DeviceFriendlyNameKey;
            CoreAudioInterop.ThrowIfFailed(propertyStore.GetValue(ref propertyKey, out propVariant));
            return propVariant.GetString();
        }
        catch (Exception ex) when (IsDeviceException(ex))
        {
            return null;
        }
        finally
        {
            CoreAudioInterop.PropVariantClear(ref propVariant);
            ReleaseCom(propertyStore);
        }
    }

    private static IMMDeviceEnumerator CreateEnumerator()
    {
        var type = Type.GetTypeFromCLSID(new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E"))
            ?? throw new InvalidOperationException("The MMDevice enumerator COM class is not registered.");
        return (IMMDeviceEnumerator)Activator.CreateInstance(type)!;
    }

    private static bool IsDeviceException(Exception ex)
    {
        return ex is COMException
            or InvalidComObjectException
            or InvalidCastException
            or InvalidOperationException
            or ObjectDisposedException
            or UnauthorizedAccessException;
    }

    private static string FormatAudioError(string operation, Exception ex)
    {
        return $"Unable to {operation} for this endpoint. {ex.Message}";
    }

    private static AudioDirection? MapDataFlow(EDataFlow flow)
    {
        return flow switch
        {
            EDataFlow.Render => AudioDirection.Output,
            EDataFlow.Capture => AudioDirection.Input,
            _ => null
        };
    }

    private static void ReleaseCom(object? comObject)
    {
        try
        {
            if (comObject is not null && Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }
        }
        catch (InvalidComObjectException)
        {
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class AudioNotificationClient : IMMNotificationClient
    {
        private readonly Action<AudioDevicesChangedEventArgs, DeviceState?> _onChanged;

        public AudioNotificationClient(Action<AudioDevicesChangedEventArgs, DeviceState?> onChanged)
        {
            _onChanged = onChanged;
        }

        public int OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            _onChanged(new AudioDevicesChangedEventArgs(null, deviceId), newState);
            return 0;
        }

        public int OnDeviceAdded(string deviceId)
        {
            _onChanged(new AudioDevicesChangedEventArgs(null, deviceId), null);
            return 0;
        }

        public int OnDeviceRemoved(string deviceId)
        {
            _onChanged(new AudioDevicesChangedEventArgs(null, deviceId), DeviceState.NotPresent);
            return 0;
        }

        public int OnDefaultDeviceChanged(EDataFlow flow, ERole role, string? defaultDeviceId)
        {
            if (role == ERole.Multimedia)
            {
                _onChanged(
                    new AudioDevicesChangedEventArgs(MapDataFlow(flow), defaultDeviceId, isDefaultChange: true),
                    null);
            }

            return 0;
        }

        public int OnPropertyValueChanged(string deviceId, PropertyKey key)
        {
            _onChanged(new AudioDevicesChangedEventArgs(null, deviceId), null);
            return 0;
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    private sealed class EndpointVolumeNotificationClient : IAudioEndpointVolumeCallback
    {
        private readonly string _deviceId;
        private readonly Action<string> _onChanged;

        public EndpointVolumeNotificationClient(string deviceId, Action<string> onChanged)
        {
            _deviceId = deviceId;
            _onChanged = onChanged;
        }

        public int OnNotify(IntPtr notifyData)
        {
            _onChanged(_deviceId);
            return 0;
        }
    }

    private sealed class EndpointVolumeSubscription : IDisposable
    {
        private readonly string _deviceId;
        private readonly IAudioEndpointVolume _endpointVolume;
        private readonly EndpointVolumeNotificationClient _callback;
        private bool _disposed;

        private EndpointVolumeSubscription(
            string deviceId,
            IAudioEndpointVolume endpointVolume,
            EndpointVolumeNotificationClient callback)
        {
            _deviceId = deviceId;
            _endpointVolume = endpointVolume;
            _callback = callback;
        }

        public static EndpointVolumeSubscription Create(
            string deviceId,
            IAudioEndpointVolume endpointVolume,
            Action<string> onChanged)
        {
            var callback = new EndpointVolumeNotificationClient(deviceId, onChanged);
            CoreAudioInterop.ThrowIfFailed(endpointVolume.RegisterControlChangeNotify(callback));
            return new EndpointVolumeSubscription(deviceId, endpointVolume, callback);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            try
            {
                _endpointVolume.UnregisterControlChangeNotify(_callback);
            }
            catch (Exception ex) when (IsDeviceException(ex))
            {
            }
            finally
            {
                ReleaseCom(_endpointVolume);
            }
        }

        public override string ToString()
        {
            return _deviceId;
        }
    }
}
