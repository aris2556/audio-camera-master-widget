using System.Reflection;
using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.Tests;

[TestClass]
public sealed class CoreAudioInteropContractTests
{
    [TestMethod]
    public void ImmDeviceCollectionUsesDocumentedInterfaceId()
    {
        Assert.AreEqual(
            new Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"),
            typeof(IMMDeviceCollection).GUID);
    }

    [TestMethod]
    public void ImmNotificationClientUsesDocumentedInterfaceId()
    {
        Assert.AreEqual(
            new Guid("7991EEC9-7E89-4D85-8390-6C703CEC60C0"),
            typeof(IMMNotificationClient).GUID);
    }

    [TestMethod]
    public void AudioEndpointVolumeCallbackUsesDocumentedInterfaceId()
    {
        Assert.AreEqual(
            new Guid("657804FA-D6AD-4496-8A60-352752AF4F89"),
            typeof(IAudioEndpointVolumeCallback).GUID);
    }

    [TestMethod]
    public void NotificationRegistrationUsesTypedCallbackInterfaces()
    {
        var endpointNotificationParameter = typeof(IMMDeviceEnumerator)
            .GetMethod(nameof(IMMDeviceEnumerator.RegisterEndpointNotificationCallback))!
            .GetParameters()
            .Single();
        var volumeNotificationParameter = typeof(IAudioEndpointVolume)
            .GetMethod(nameof(IAudioEndpointVolume.RegisterControlChangeNotify))!
            .GetParameters()
            .Single();

        Assert.AreEqual(typeof(IMMNotificationClient), endpointNotificationParameter.ParameterType);
        Assert.AreEqual(typeof(IAudioEndpointVolumeCallback), volumeNotificationParameter.ParameterType);
    }

    [TestMethod]
    public void AudioEndpointVolumeMethodsUseDocumentedVtableOrder()
    {
        var methodNames = typeof(IAudioEndpointVolume)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .OrderBy(method => method.MetadataToken)
            .Select(method => method.Name)
            .ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                nameof(IAudioEndpointVolume.RegisterControlChangeNotify),
                nameof(IAudioEndpointVolume.UnregisterControlChangeNotify),
                nameof(IAudioEndpointVolume.GetChannelCount),
                nameof(IAudioEndpointVolume.SetMasterVolumeLevel),
                nameof(IAudioEndpointVolume.SetMasterVolumeLevelScalar),
                nameof(IAudioEndpointVolume.GetMasterVolumeLevel),
                nameof(IAudioEndpointVolume.GetMasterVolumeLevelScalar),
                nameof(IAudioEndpointVolume.SetChannelVolumeLevel),
                nameof(IAudioEndpointVolume.SetChannelVolumeLevelScalar),
                nameof(IAudioEndpointVolume.GetChannelVolumeLevel),
                nameof(IAudioEndpointVolume.GetChannelVolumeLevelScalar),
                nameof(IAudioEndpointVolume.SetMute),
                nameof(IAudioEndpointVolume.GetMute),
                nameof(IAudioEndpointVolume.GetVolumeStepInfo),
                nameof(IAudioEndpointVolume.VolumeStepUp),
                nameof(IAudioEndpointVolume.VolumeStepDown),
                nameof(IAudioEndpointVolume.QueryHardwareSupport),
                nameof(IAudioEndpointVolume.GetVolumeRange),
            },
            methodNames);
    }
}
