#if UNITY_EDITOR
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

public class SpriteAssetSelectionAdjuster : EditorWindow
{
    [Header("수정할 BX/BY 값")]
    public float newBX = 0f;
    public float newBY = 0f;

    [MenuItem("Tools/TMP/Selected SpriteAssets BX BY Adjuster")]
    static void Init()
    {
        SpriteAssetSelectionAdjuster window = (SpriteAssetSelectionAdjuster)GetWindow(typeof(SpriteAssetSelectionAdjuster));
        window.titleContent = new GUIContent("TMP Selected BX/BY Adjuster");
        window.Show();
    }

    private void OnGUI()
    {
        GUILayout.Label("선택된 Asset들의 BX/BY 수정", EditorStyles.boldLabel);
        newBX = EditorGUILayout.FloatField("BX (horiBearingX)", newBX);
        newBY = EditorGUILayout.FloatField("BY (horiBearingY)", newBY);

        if (GUILayout.Button("선택 Asset 수정"))
        {
            AdjustSelectedAssets();
        }
    }

    private void AdjustSelectedAssets()
    {
        // 현재 선택된 Asset 가져오기
        Object[] selection = Selection.objects;
        if (selection.Length == 0)
        {
            Debug.LogWarning("수정할 TMP Sprite Asset을 선택하세요.");
            return;
        }

        foreach (var obj in selection)
        {
            TMP_SpriteAsset asset = obj as TMP_SpriteAsset;
            if (asset == null) continue;
            if (asset.spriteCharacterTable == null || asset.spriteGlyphTable == null) continue;

            Debug.Log($"[AdjustSelected] Asset: {asset.name}");

            foreach (var spriteChar in asset.spriteCharacterTable)
            {
                if (spriteChar == null) continue;

                TMP_SpriteGlyph glyph = asset.spriteGlyphTable
                    .FirstOrDefault(g => g != null && g.index == spriteChar.glyphIndex);

                if (glyph == null) continue;

                GlyphMetrics metrics = glyph.metrics;
                metrics.horizontalBearingX = newBX;
                metrics.horizontalBearingY = newBY;
                glyph.metrics = metrics;

                int glyphTableIndex = asset.spriteGlyphTable.IndexOf(glyph);
                if (glyphTableIndex >= 0)
                    asset.spriteGlyphTable[glyphTableIndex] = glyph;

                Debug.Log($"  수정 완료: {spriteChar.name} BX={newBX}, BY={newBY}");
            }

            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("선택된 Asset 모두 수정 완료!");
    }
}
#endif
