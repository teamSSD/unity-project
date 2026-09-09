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
        var cropText = Regex.Match(prefab,
            @"m_Father: \{fileID: 2423252458153559830\}[\s\S]*?m_fontAsset: \{fileID: 11400000, guid: ([a-f0-9]+), type: 2\}[\s\S]*?m_fontColor: \{r: 0, g: 0, b: 0, a: 1\}[\s\S]*?m_fontSize: (\d+)[\s\S]*?m_fontStyle: (\d+)");
        Assert.That(cropText.Success, Is.True, "작물명 TMP 설정을 찾지 못했습니다.");
        Assert.That(cropText.Groups[1].Value, Is.EqualTo("0545ace1e04104948bd80ac2267053bf"),
            "작물명은 locked 안내문과 같은 SUIT Bold를 사용해야 합니다.");
        Assert.That(cropText.Groups[2].Value, Is.EqualTo("36"));
        Assert.That(cropText.Groups[3].Value, Is.EqualTo("0"));

        var farmScriptPath = Path.Combine(Application.dataPath, "Scripts", "Unity", "Garden", "Farm.cs");
        var farmScript = File.ReadAllText(farmScriptPath);
        StringAssert.Contains("FarmLabelLayout.VisibleBounds(sprite.vertices, sprite.bounds)", farmScript);
        StringAssert.Contains("FarmLabelLayout.AboveCrop", farmScript);
        StringAssert.Contains("FarmLabelLayout.AboveElement", farmScript);
        StringAssert.Contains("FarmLabelLayout.HalfHeightInAncestor", farmScript);
        StringAssert.Contains("actionPrompt.text = \"\";", farmScript);
    }
}
