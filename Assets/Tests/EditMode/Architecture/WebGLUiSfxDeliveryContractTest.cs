using System.IO;
using NUnit.Framework;
using UnityEngine;

public class WebGLUiSfxDeliveryContractTest
{
    [Test]
    public void UiSfx_AreNotSerializedIntoTheBootstrapSoundManager()
    {
        var managersPath = Path.Combine(Application.dataPath, "Scenes", "ForReal", "Managers.unity");
        var managers = File.ReadAllText(managersPath);

        Assert.That(managers, Does.Not.Contain("uiBookSfx:"));
        Assert.That(managers, Does.Not.Contain("buttonClickSfx:"));
        Assert.That(managers, Does.Not.Contain("bgmMall:"));
        Assert.That(managers, Does.Not.Contain("bgmCooking:"));
        Assert.That(managers, Does.Not.Contain("bgmNight:"));
        Assert.That(managers, Does.Not.Contain("bgmGarden:"));
    }

    [Test]
    public void WebGLUiSfx_UsesBrowserAudioAndStreamingAssets()
    {
        var pluginPath = Path.Combine(Application.dataPath, "Plugins", "WebGL", "AftertasteUiSfx.jslib");
        var plugin = File.ReadAllText(pluginPath);
        var soundManagerPath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Common", "SoundManager.cs");
        var soundManager = File.ReadAllText(soundManagerPath);

        Assert.That(plugin, Does.Contain("AftertastePlayUiSfx"));
        Assert.That(plugin, Does.Contain("AftertastePlayBgm"));
        Assert.That(plugin, Does.Contain("document.createElement(\"audio\")"));
        Assert.That(soundManager, Does.Contain("#if UNITY_WEBGL && !UNITY_EDITOR"));
        Assert.That(soundManager, Does.Contain("AftertastePlayUiSfx"));
        Assert.That(soundManager, Does.Not.Contain("Resources.Load<AudioClip>"));
        Assert.That(soundManager, Does.Contain("UnityWebRequestMultimedia.GetAudioClip"));
        Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, "Audio", "UI", "sfx_ui_book.mp3")), Is.True);
        Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, "Audio", "UI", "sfx_ui_button_click.mp3")), Is.True);
        Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, "Audio", "BGM", "bgm_mall_theme.mp3")), Is.True);
        Assert.That(File.Exists(Path.Combine(Application.streamingAssetsPath, "Audio", "BGM", "bgm_preperation_theme.mp3")), Is.True);
        Assert.That(File.Exists(Path.Combine(Application.dataPath, "Resources", "Audio", "UI", "sfx_ui_book.mp3")), Is.False);
        Assert.That(File.Exists(Path.Combine(Application.dataPath, "Resources", "Audio", "UI", "sfx_ui_button_click.mp3")), Is.False);
    }
}
