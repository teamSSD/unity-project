using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>일회성. 사용중인 SUIT SDF 폰트 5개를 Dynamic 모드로 전환.
/// - Static (11,405 glyphs pre-baked, 각 37MB) → Dynamic (필요 시 runtime rasterize)
/// - ClearFontAssetData(true)로 atlas 크기 0, glyph/character 테이블 초기화
/// - 원본 ttf가 dynamic rendering의 소스라 반드시 유지되어야 함
/// - Boot 시 FontPreWarmer가 자주 쓰는 글자 미리 add해 실 게임플레이 hitch 방지</summary>
public static class _TempFontToDynamic
{
    private static readonly string[] Paths =
    {
        "Assets/Resources/TextMesh Pro/Fonts/SUIT-Regular SDF.asset",
        "Assets/Resources/TextMesh Pro/Fonts/SUIT-Medium SDF.asset",
        "Assets/Resources/TextMesh Pro/Fonts/SUIT-Bold SDF.asset",
        "Assets/Resources/TextMesh Pro/Fonts/SUIT-SemiBold SDF.asset",
        "Assets/Resources/TextMesh Pro/Fonts/SUIT-ExtraBold SDF.asset",
    };

    [MenuItem("Tools/Build/Convert Fonts to Dynamic")]
    public static void Run()
    {
        foreach (var path in Paths)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (font == null) { Debug.LogError($"[Font] 미발견: {path}"); continue; }

            var beforeChars = font.characterTable?.Count ?? 0;
            var beforeGlyphs = font.glyphTable?.Count ?? 0;

            font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            font.ClearFontAssetData(setAtlasSizeToZero: true);
            EditorUtility.SetDirty(font);

            Debug.Log($"[Font] Converted: {System.IO.Path.GetFileNameWithoutExtension(path)} " +
                      $"(chars {beforeChars} → 0, glyphs {beforeGlyphs} → 0, atlas → 0×0)");
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Font] Dynamic 전환 완료. Boot 시 pre-warm 권장.");
    }
}
