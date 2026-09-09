using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

public class EarlyEndButtonLayoutContractTest
{
    // The visual baseline is intentionally close to the clock, not an oversized safe gap.
    private static readonly Regex EarlyEndButtonPosition = new(
        @"m_Name: EarlyEndButton[\s\S]*?m_AnchoredPosition: \{x: -26, y: (-?\d+)\}",
        RegexOptions.Compiled);

    [TestCase("Cooking.unity")]
    [TestCase("CookingTutorial.unity")]
    public void EarlyEndButton_IsBelowTheClockSafeArea(string sceneName)
    {
        var scenePath = Path.Combine(Application.dataPath, "Scenes", "ForReal", sceneName);
        var match = EarlyEndButtonPosition.Match(File.ReadAllText(scenePath));

        Assert.That(match.Success, Is.True, $"{sceneName}의 EarlyEndButton 위치를 찾지 못했습니다.");
        Assert.That(int.Parse(match.Groups[1].Value), Is.LessThanOrEqualTo(-350),
            $"{sceneName}의 조기 종료 버튼은 시계 안전 영역 아래에 있어야 합니다.");
    }

    [Test]
    public void EarlyEndButton_CanForceCloseAfterClockEnd_WithoutDuplicateSessionEnd()
    {
        var managerPath = Path.Combine(
            Application.dataPath, "Scripts", "Unity", "Cooking", "CustomerManager.cs");
        var source = File.ReadAllText(managerPath);

        Assert.That(source, Does.Contain("private bool hasEnded;"));
        Assert.That(source, Does.Contain("if (hasEnded) return;"));
        Assert.That(source, Does.Not.Contain("if (!isOpen) return;"));
        Assert.That(source, Does.Contain("private void FinishSession()"));
    }
}
