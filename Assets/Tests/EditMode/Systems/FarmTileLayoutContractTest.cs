using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class FarmTileLayoutContractTest
{
    private const string FarmTilePrefab = "Assets/Bundles/Prefabs/garden/FarmTile.prefab";

    [Test]
    public void CropLabel_IsBelowCrop_AndGaugeUsesCompactSize()
    {
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", FarmTilePrefab));
        var prefab = File.ReadAllText(path);

        var nameCanvas = Regex.Match(prefab,
            @"m_Father: \{fileID: 2423252458153559830\}[\s\S]*?m_AnchoredPosition: \{x: 0, y: (-?\d+)\}[\s\S]*?m_SizeDelta: \{x: 400, y: (\d+)\}");
        Assert.That(nameCanvas.Success, Is.True, "FarmTile 작물명 캔버스의 레이아웃을 찾지 못했습니다.");
        Assert.That(int.Parse(nameCanvas.Groups[1].Value), Is.GreaterThanOrEqualTo(150),
            "작물명은 작물 스프라이트 바로 위에 배치되어야 합니다.");
        Assert.That(int.Parse(nameCanvas.Groups[2].Value), Is.EqualTo(100),
            "작물명 영역은 원래의 가독성 있는 높이를 유지해야 합니다.");

        var gaugeWidth = Regex.Match(prefab,
            @"propertyPath: m_SizeDelta.x\s+value: (\d+)[\s\S]*?propertyPath: m_SizeDelta.y\s+value: (\d+)");
        Assert.That(gaugeWidth.Success, Is.True, "FarmTile 성장 게이지의 크기를 찾지 못했습니다.");
        Assert.That(int.Parse(gaugeWidth.Groups[1].Value), Is.LessThanOrEqualTo(130));
        Assert.That(int.Parse(gaugeWidth.Groups[2].Value), Is.LessThanOrEqualTo(130));
    }
}
