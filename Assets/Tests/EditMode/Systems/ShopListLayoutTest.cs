using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

public class ShopListLayoutTest
{
    [Test]
    public void Rebuild_UsesEveryRuntimeRowForScrollableContentHeight()
    {
        var root = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));
        try
        {
            var viewport = new GameObject("Viewport", typeof(RectTransform)).GetComponent<RectTransform>();
            viewport.SetParent(root.transform, false);
            viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 300f);

            var content = new GameObject(
                "Content",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup)).GetComponent<RectTransform>();
            content.SetParent(viewport, false);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.spacing = 6f;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var scroll = root.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;

            for (int i = 0; i < 20; i++)
            {
                var row = new GameObject($"Row {i}", typeof(RectTransform), typeof(LayoutElement));
                row.transform.SetParent(content, false);
                row.GetComponent<LayoutElement>().preferredHeight = 150f;
            }

            float required = ShopListLayout.Rebuild(content);

            Assert.That(required, Is.EqualTo(3122f).Within(0.01f));
            Assert.That(content.rect.height, Is.EqualTo(3122f).Within(0.01f));
            Assert.That(scroll.verticalNormalizedPosition, Is.EqualTo(1f).Within(0.001f));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
