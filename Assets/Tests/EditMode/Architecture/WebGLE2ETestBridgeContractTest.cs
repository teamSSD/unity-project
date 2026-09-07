using System.IO;
using NUnit.Framework;
using UnityEngine;

public class WebGLE2ETestBridgeContractTest
{
    [Test]
    public void E2EBridge_IsDevelopmentOnly_AndDoesNotOfferGameInputCommands()
    {
        var path = Path.Combine(Application.dataPath, "Scripts", "Unity", "Dev", "E2E", "AftertasteE2ETestBridge.cs");
        var source = File.ReadAllText(path);

        Assert.That(source, Does.Contain("#if AFTERTASTE_E2E"));
        Assert.That(source, Does.Contain("case \"snapshot\""));
        Assert.That(source, Does.Contain("case \"mark\""));
        Assert.That(source, Does.Contain("case \"timeScale\""));
        Assert.That(source, Does.Not.Contain("case \"move\""));
        Assert.That(source, Does.Not.Contain("case \"click\""));
    }

    [Test]
    public void DevelopmentWebGLBuild_RegistersInstance_AndKeepsBoundedEventLog()
    {
        var pluginPath = Path.Combine(Application.dataPath, "Plugins", "WebGL", "AftertasteE2E.jslib");
        var plugin = File.ReadAllText(pluginPath);
        var postprocessorPath = Path.Combine(Application.dataPath, "Scripts", "Editor", "WebGLE2EBuildPostprocessor.cs");
        var postprocessor = File.ReadAllText(postprocessorPath);

        Assert.That(plugin, Does.Contain("AftertasteE2EEmit"));
        Assert.That(postprocessor, Does.Contain("BuildOptions.Development"));
        Assert.That(postprocessor, Does.Contain("_e2e/"));
        var buildAutomationPath = Path.Combine(Application.dataPath, "Editor", "BuildAutomation.cs");
        var buildAutomation = File.ReadAllText(buildAutomationPath);
        Assert.That(buildAutomation, Does.Contain("AFTERTASTE_E2E"));
        Assert.That(postprocessor, Does.Contain("window.AftertasteE2E"));
        Assert.That(postprocessor, Does.Contain("window.AftertasteUnityInstance = unityInstance"));
        Assert.That(postprocessor, Does.Contain("window.AftertasteUnityInstance.SendMessage"));
        Assert.That(postprocessor, Does.Contain("var events = []"));
        Assert.That(postprocessor, Does.Contain("events.length > 500"));
    }
}
