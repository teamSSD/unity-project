using TMPro;
using UnityEngine;

public static class InteractPromptUI
{
    private static GameObject promptObj;
    private static TMP_Text tmpText;

    public static void Show(string text)
    {
        EnsureCreated();
        tmpText.text = text;
        promptObj.SetActive(true);
    }

    public static void Hide()
    {
        if (promptObj != null)
            promptObj.SetActive(false);
    }

    static void EnsureCreated()
    {
        if (promptObj != null) return;

        var canvasObj = new GameObject("InteractPromptCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        promptObj = new GameObject("InteractPrompt", typeof(RectTransform));
        promptObj.transform.SetParent(canvas.transform, false);

        var rect = promptObj.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, -458f);
        rect.sizeDelta = new Vector2(800f, 60f);

        tmpText = promptObj.AddComponent<TextMeshProUGUI>();
        tmpText.fontSize = 52;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.color = Color.white;

        promptObj.SetActive(false);
    }
}
