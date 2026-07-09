using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>일회성. Unity의 AssetDatabase.GetDependencies로 재귀 참조 그래프를 만든 뒤
/// 어디에서도 참조되지 않는 텍스처(png/jpg)를 찾음. Bash grep보다 정확 —
/// prefab variant override, nested prefab, SO field, AnimationClip keyframe, Material 등 모두 커버.
/// Resources/ 폴더 안은 auto-include 되므로 제외.
/// 결과: tmp/build-prep/orphan_textures_authoritative.md</summary>
public static class _TempFindOrphanTextures
{
    private const string OutputPath = "tmp/build-prep/orphan_textures_authoritative.md";

    [MenuItem("Tools/Build/Find Orphan Textures")]
    public static void Run()
    {
        EditorUtility.DisplayProgressBar("Orphan Scan", "루트 asset 수집…", 0f);

        // 1. 모든 root asset 수집 — 이들의 dependency를 재귀 순회하면 사용 중인 asset 전체 커버.
        var rootFilters = new[]
        {
            "t:Scene",
            "t:Prefab",
            "t:ScriptableObject",   // FoodData, RecipeData, CatalogSO, TutorialStepData 등
            "t:AnimationClip",
            "t:AnimatorController",
            "t:AnimatorOverrideController",
            "t:Material",
            "t:Shader",
            "t:PlayableAsset",      // Timeline
            "t:Font",
            "t:VideoClip",
        };

        var rootPaths = new HashSet<string>();
        foreach (var filter in rootFilters)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var p = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(p)) rootPaths.Add(p);
            }
        }
        Debug.Log($"[Orphan] Root asset {rootPaths.Count}개 수집");

        // 2. 각 root의 GetDependencies(recursive:true)로 실질 참조 그래프 union.
        EditorUtility.DisplayProgressBar("Orphan Scan", "dependency 그래프 순회 중…", 0.3f);
        var reachable = new HashSet<string>();
        int idx = 0;
        foreach (var rootPath in rootPaths)
        {
            EditorUtility.DisplayProgressBar("Orphan Scan",
                $"dependency 순회 ({idx}/{rootPaths.Count})", 0.3f + 0.5f * idx / rootPaths.Count);
            var deps = AssetDatabase.GetDependencies(rootPath, recursive: true);
            foreach (var dep in deps) reachable.Add(dep);
            idx++;
        }
        Debug.Log($"[Orphan] Reachable asset {reachable.Count}개");

        // 3. 모든 텍스처 열거 후 reachable 제외.
        EditorUtility.DisplayProgressBar("Orphan Scan", "텍스처 순회 중…", 0.85f);
        var textureGuids = AssetDatabase.FindAssets("t:Texture2D");
        var orphans = new List<(string path, long size)>();
        foreach (var guid in textureGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) continue;
            if (reachable.Contains(path)) continue;
            // Resources/ 안 파일은 auto-include (Resources.Load 대상) → 제외.
            if (path.Contains("/Resources/")) continue;
            // StreamingAssets/Editor Default Resources는 프로젝트에 없지만 방어적으로 필터.
            if (path.Contains("/StreamingAssets/") || path.Contains("/Editor Default Resources/")) continue;
            // Editor 폴더는 빌드 제외라 orphan 판정 무의미 (원래도 안 포함됨).
            if (path.Contains("/Editor/")) continue;
            long size = 0;
            try { size = new FileInfo(path).Length; } catch { }
            orphans.Add((path, size));
        }

        orphans.Sort((a, b) => b.size.CompareTo(a.size));

        // 4. Report
        var sb = new StringBuilder();
        sb.AppendLine("# Orphan Textures — AssetDatabase 기반 감사");
        sb.AppendLine();
        sb.AppendLine($"Root asset: {rootPaths.Count}, Reachable: {reachable.Count}, Textures 전체: {textureGuids.Length}, **Orphan: {orphans.Count}**");
        sb.AppendLine();
        sb.AppendLine("방법: Unity `AssetDatabase.GetDependencies(path, recursive: true)`로 모든 씬/프리팹/SO/AnimClip/Material/Timeline 등의 재귀 dependency 수집. reachable set에 안 잡히면 orphan. Resources/StreamingAssets/Editor Default Resources는 자동 포함이라 제외.");
        sb.AppendLine();
        sb.AppendLine($"**총 크기**: {orphans.Sum(o => o.size) / (1024.0 * 1024.0):F2} MB");
        sb.AppendLine();
        sb.AppendLine("| 파일 | 크기 (KB) |");
        sb.AppendLine("|---|---:|");
        foreach (var (path, size) in orphans)
            sb.AppendLine($"| `{path}` | {size / 1024.0:F1} |");

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        File.WriteAllText(OutputPath, sb.ToString());
        EditorUtility.ClearProgressBar();
        Debug.Log($"[Orphan] Orphan {orphans.Count}개 발견 → {OutputPath}");
    }
}
