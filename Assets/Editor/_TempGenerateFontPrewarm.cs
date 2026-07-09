using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>일회성. 게임에 등장할 모든 unique 문자를 수집해 font_prewarm_chars.txt로 저장.
/// FontPreWarmer가 Boot 시 이걸 로드해 5개 SUIT 폰트에 TryAddCharacters 호출 → runtime hitch 방지.
/// 새 텍스트(메뉴명 추가/대화 변경) 나오면 이 tool 재실행 후 재빌드.</summary>
public static class _TempGenerateFontPrewarm
{
    private const string OutputPath = "Assets/Resources/font_prewarm_chars.txt";

    [MenuItem("Tools/Build/Generate Font Prewarm Chars")]
    public static void Run()
    {
        var chars = new HashSet<char>();

        // ── 1. 기본 ASCII printable + 일반 기호
        for (char c = ' '; c <= '~'; c++) chars.Add(c);
        foreach (var c in "원₩달러$€¥%°℃『』「」·…—–‘’“”←→↑↓★☆♥♪♬✓✗") chars.Add(c);

        // ── 2. 모든 ScriptableObject의 string 필드 수집 (FoodData, RecipeData, DeliveryNpcData 등)
        int soCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            var so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so == null) continue;
            AddStringFields(so, chars);
            soCount++;
        }

        // ── 3. driveAssets/dataTables 하위 TextAsset (CSV/JSON 데이터)
        int taCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:TextAsset"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            if (!path.Contains("driveAssets/") && !path.Contains("Bundles/")) continue;
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta == null || ta.text == null) continue;
            foreach (var c in ta.text) chars.Add(c);
            taCount++;
        }

        // ── 4. 모든 Scene/Prefab의 TextMeshPro 컴포넌트 초기 텍스트 (고정 UI 라벨)
        int tmpCount = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) continue;
            foreach (var tmp in go.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (tmp.text != null) foreach (var c in tmp.text) chars.Add(c);
                tmpCount++;
            }
        }

        // ── 5. 파일 저장 (제어 문자 제외 후 정렬)
        var sorted = new string(chars.Where(c => c >= 32).OrderBy(c => c).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, sorted);
        AssetDatabase.ImportAsset(OutputPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[Prewarm] {sorted.Length} unique chars 저장 → {OutputPath}");
        Debug.Log($"[Prewarm] 소스: SO {soCount}개, TextAsset {taCount}개, TMP 컴포넌트 {tmpCount}개");
    }

    /// <summary>Reflection으로 SO의 모든 string 필드 순회 (public/private).</summary>
    private static void AddStringFields(object obj, HashSet<char> chars)
    {
        if (obj == null) return;
        var type = obj.GetType();
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public |
                                             System.Reflection.BindingFlags.NonPublic |
                                             System.Reflection.BindingFlags.Instance))
        {
            if (field.FieldType == typeof(string))
            {
                var s = field.GetValue(obj) as string;
                if (s != null) foreach (var c in s) chars.Add(c);
            }
            else if (field.FieldType == typeof(System.Collections.Generic.List<string>) ||
                     field.FieldType == typeof(string[]))
            {
                var list = field.GetValue(obj) as System.Collections.IEnumerable;
                if (list != null)
                    foreach (var item in list)
                        if (item is string s2)
                            foreach (var c in s2) chars.Add(c);
            }
        }
    }
}
