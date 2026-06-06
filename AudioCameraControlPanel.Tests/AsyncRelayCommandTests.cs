using AudioCameraControlPanel.ViewModels;

namespace AudioCameraControlPanel.Tests;

[TestClass]
public sealed class AsyncRelayCommandTests
{
    [TestMethod]
    public async Task ExecuteReportsExceptionsThroughCallback()
    {
        var reportedException = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        var command = new AsyncRelayCommand(
            () => throw new InvalidOperationException("boom"),
            onError: exception =>
            {
                reportedException.SetResult(exception);
                return Task.CompletedTask;
            });

        command.Execute(null);

        var exception = await reportedException.Task.WaitAsync(TimeSpan.FromSeconds(1));
        Assert.IsInstanceOfType<InvalidOperationException>(exception);
        Assert.AreEqual("boom", exception.Message);
    }
}
