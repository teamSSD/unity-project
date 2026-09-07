using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class FarmTileLayoutContractTest
{
    private const string FarmTilePrefab = "Assets/Bundles/Prefabs/garden/FarmTile.prefab";

    [Test]
    public void CropLabel_UsesPromptStyle_AndIsMeasuredAboveCrop()
    {
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FarmTilePrefab));
        var prefab = File.ReadAllText(path);

        var nameCanvas = Regex.Match(prefab,
            @"m_Father: \{fileID: 2423252458153559830\}[\s\S]*?m_AnchoredPosition: \{x: 0, y: (-?\d+)\}[\s\S]*?m_SizeDelta: \{x: 250, y: (\d+)\}");
        Assert.That(nameCanvas.Success, Is.True, "FarmTile 작물명 캔버스의 레이아웃을 찾지 못했습니다.");
        Assert.That(int.Parse(nameCanvas.Groups[1].Value), Is.Zero);
        Assert.That(int.Parse(nameCanvas.Groups[2].Value), Is.EqualTo(50),
            "작물명은 근접 안내문과 동일한 크기를 유지해야 합니다.");
        StringAssert.Contains("m_fontColor: {r: 0, g: 0, b: 0, a: 1}", prefab);
        StringAssert.Contains("m_fontSize: 36", prefab);

        var farmScriptPath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Garden", "Farm.cs");
        var farmScript = File.ReadAllText(farmScriptPath);
        StringAssert.Contains("FarmLabelLayout.VisibleBounds(sprite.vertices, sprite.bounds)", farmScript);
        StringAssert.Contains("FarmLabelLayout.AboveCrop", farmScript);
        StringAssert.Contains("actionPrompt.text = \"\";", farmScript);
    }
}
