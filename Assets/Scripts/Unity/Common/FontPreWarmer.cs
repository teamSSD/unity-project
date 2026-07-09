using UnityEngine;

/// <summary>Boot 시 SUIT 폰트 5개에 게임에 등장할 문자를 미리 add해 runtime hitch 방지.
/// - 폰트는 Static → Dynamic 전환됨 (atlas 0×0으로 시작, TryAddCharacters로 확장)
/// - font_prewarm_chars.txt는 _TempGenerateFontPrewarm Editor tool로 생성
/// - BootLoader에서 EnsureAll 직후 호출</summary>
public static class FontPreWarmer
{
    private const string CharsResourceName = "font_prewarm_chars";
    private static readonly string[] FontResourcePaths =
    {
        "TextMesh Pro/Fonts/SUIT-Regular SDF",
        "TextMesh Pro/Fonts/SUIT-Medium SDF",
        "TextMesh Pro/Fonts/SUIT-Bold SDF",
        "TextMesh Pro/Fonts/SUIT-SemiBold SDF",
        "TextMesh Pro/Fonts/SUIT-ExtraBold SDF",
    };

    private static bool _done;

    /// <summary>재호출 안전. 최초 한 번만 실행.</summary>
    public static void WarmAll()
    {
        if (_done) return;
        _done = true;

        var chars = Resources.Load<TextAsset>(CharsResourceName);
        if (chars == null || string.IsNullOrEmpty(chars.text))
        {
            Debug.LogWarning($"[FontPreWarmer] {CharsResourceName}.txt 미발견 — pre-warm skip");
            return;
        }

        int totalAdded = 0;
        foreach (var path in FontResourcePaths)
        {
            var font = Resources.Load<TMPro.TMP_FontAsset>(path);
            if (font == null) { Debug.LogWarning($"[FontPreWarmer] 폰트 미발견: {path}"); continue; }
            font.TryAddCharacters(chars.text, out _);
            totalAdded += font.characterTable?.Count ?? 0;
        }
        Debug.Log($"[FontPreWarmer] {chars.text.Length} 문자 × {FontResourcePaths.Length} 폰트 pre-warm 완료 (총 {totalAdded} entries)");
    }
}
