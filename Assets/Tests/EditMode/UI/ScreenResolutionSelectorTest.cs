using NUnit.Framework;
using UnityEngine;

public class ScreenResolutionSelectorTest
{
    [Test]
    public void WebGL_DelegatesResolutionToResponsiveBrowserCanvas()
    {
        Assert.That(
            ScreenResolutionSelector.SupportsManualResolution(RuntimePlatform.WebGLPlayer),
            Is.False);
    }

    [TestCase(RuntimePlatform.WindowsPlayer)]
    [TestCase(RuntimePlatform.OSXPlayer)]
    [TestCase(RuntimePlatform.LinuxPlayer)]
    public void DesktopPlayer_KeepsManualResolutionSelection(RuntimePlatform platform)
    {
        Assert.That(ScreenResolutionSelector.SupportsManualResolution(platform), Is.True);
    }
}
