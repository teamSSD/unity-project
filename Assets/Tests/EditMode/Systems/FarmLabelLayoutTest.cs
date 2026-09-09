using NUnit.Framework;
using UnityEngine;

public class FarmLabelLayoutTest
{
    [Test]
    public void AboveCrop_UsesTightMeshTop_NotTransparentSpriteRectTop()
    {
        // 새싹은 원본 rect(상단 0.69)의 하단에만 그려진다고 가정한다.
        var transparentRect = new Bounds(Vector2.zero, new Vector2(1f, 1.38f));
        var tightMesh = new[]
        {
            new Vector2(-0.2f, -0.25f), new Vector2(0.2f, -0.25f),
            new Vector2(0.15f, 0.12f), new Vector2(-0.15f, 0.12f),
        };

        var visible = FarmLabelLayout.VisibleBounds(tightMesh, transparentRect);
        var label = FarmLabelLayout.AboveCrop(
            new Vector3(0f, 0.3f, 0f), Vector3.one, visible, labelHalfHeight: 0.25f, clearance: 0.1f);

        Assert.That(visible.max.y, Is.EqualTo(0.12f).Within(0.0001f));
        Assert.That(label.y, Is.EqualTo(0.77f).Within(0.0001f));
        Assert.That(label.y, Is.LessThan(transparentRect.max.y + 0.3f),
            "투명 여백이 큰 원본 sprite rect를 기준으로 상단으로 튀면 안 됩니다.");
    }

    [Test]
    public void AboveCrop_UsesTheUpperEdge_WhenSpriteIsFlippedVertically()
    {
        var visible = new Bounds(Vector2.zero, new Vector2(0.4f, 0.8f));
        var label = FarmLabelLayout.AboveCrop(
            Vector3.zero, new Vector3(1f, -1f, 1f), visible, labelHalfHeight: 0.25f, clearance: 0.1f);

        // y scale이 음수이면 local min.y가 월드 상단으로 변환된다: (-0.4) * (-1) = 0.4.
        Assert.That(label.y, Is.EqualTo(0.75f).Within(0.0001f));
    }

    [Test]
    public void AboveElement_StacksGaugeAboveLabelWithClearance()
    {
        var labelCenter = new Vector3(0.4f, 2f, 0f);
        var gaugeCenter = FarmLabelLayout.AboveElement(
            labelCenter,
            lowerHalfHeight: 0.5f,
            upperHalfHeight: 0.65f,
            clearance: 0.1f);

        Assert.That(gaugeCenter, Is.EqualTo(new Vector3(0.4f, 3.25f, 0f)));
        Assert.That(gaugeCenter.y - 0.65f, Is.EqualTo(labelCenter.y + 0.5f + 0.1f).Within(0.0001f),
            "게이지 하단은 작물명 상단보다 항상 지정 여백만큼 위에 있어야 합니다.");
    }
}
