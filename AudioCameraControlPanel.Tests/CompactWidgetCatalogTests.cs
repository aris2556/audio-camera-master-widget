using AudioCameraControlPanel.Models;
using AudioCameraControlPanel.Services;

namespace AudioCameraControlPanel.Tests;

[TestClass]
public sealed class CompactWidgetCatalogTests
{
    [TestMethod]
    public void AllReturnsFiveUniqueCompactWidgets()
    {
        var widgets = CompactWidgetCatalog.All;

        Assert.HasCount(5, widgets);
        CollectionAssert.AreEquivalent(
            new[]
            {
                CompactWidgetKind.Master,
                CompactWidgetKind.Output,
                CompactWidgetKind.Input,
                CompactWidgetKind.Camera,
                CompactWidgetKind.Settings
            },
            widgets.Select(widget => widget.Kind).ToArray());
    }

    [TestMethod]
    public void GetReturnsOutputWidgetMetadata()
    {
        var widget = CompactWidgetCatalog.Get(CompactWidgetKind.Output);

        Assert.AreEqual("Output widget", widget.Title);
        StringAssert.Contains(widget.Description, "volume");
    }

    [TestMethod]
    public void GetReturnsSmallMasterWidgetWithoutCameraPreview()
    {
        var widget = CompactWidgetCatalog.Get(CompactWidgetKind.Master);

        Assert.AreEqual("Master widget", widget.Title);
        StringAssert.Contains(widget.Description, "No camera preview");
    }
}
