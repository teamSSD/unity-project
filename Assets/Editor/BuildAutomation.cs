using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 원클릭 WebGL 빌드 automation.
/// - Build Settings 씬 리스트 자동 수집
/// - 출력: Builds/WebGL/{version}/{yyyyMMdd_HHmmss}/
/// - 빌드 후 요약 (파일별 크기, 총 크기, Brotli 예상 크기) 로그
/// </summary>
public static class BuildAutomation
{
    private const string OutputRoot = "Builds/WebGL";

    [MenuItem("Tools/Build/WebGL — Release")]
    public static void BuildWebGLRelease() => Build(development: false);

    [MenuItem("Tools/Build/WebGL — Development")]
    public static void BuildWebGLDevelopment() => Build(development: true);

    [MenuItem("Tools/Build/WebGL — E2E (Development)")]
    public static void BuildWebGLE2E() => Build(development: true, outputTag: "e2e");

    [MenuItem("Tools/Build/WebGL — E2E Long-run (Development)")]
    public static void BuildWebGLE2ELongRun() => Build(development: true, outputTag: "e2e_longrun");

    private static void Build(bool development, string outputTag = null)
    {
        bool e2e = outputTag == "e2e" || outputTag == "e2e_longrun";
        bool longRunE2E = outputTag == "e2e_longrun";
        // 씬 목록은 EditorBuildSettings에서 활성화된 것만
        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        if (scenes.Length == 0)
        {
            EditorUtility.DisplayDialog("Build 실패", "Build Settings에 활성 씬 없음.", "OK");
            return;
        }

        string version = PlayerSettings.bundleVersion;
        string tag = outputTag ?? (development ? "dev" : "release");
        string outDir = Path.Combine(OutputRoot, $"{version}_{tag}", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(outDir);

        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outDir,
            target = BuildTarget.WebGL,
            options = development ? BuildOptions.Development : BuildOptions.None,
            extraScriptingDefines = !e2e ? System.Array.Empty<string>()
                : longRunE2E ? new[] { "AFTERTASTE_E2E", "AFTERTASTE_E2E_LONGRUN" }
                : new[] { "AFTERTASTE_E2E" },
        };

        // 활성 타겟이 WebGL이 아니면 스위칭 (첫 빌드 시 시간 걸림)
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
        {
            Debug.Log("[Build] WebGL 타겟으로 스위칭 (첫 빌드 시 오래 걸릴 수 있음)…");
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
        }

        Debug.Log($"[Build] 시작: {tag}, {scenes.Length} 씬, → {outDir}");
        var report = BuildPipeline.BuildPlayer(options);

        LogSummary(report, outDir);
        // 외부 E2E 실행기는 출력 폴더 생성만으로 빌드 완료를 판단하면 안 된다.
        // WebGL 파일과 postprocess 주입까지 모두 끝난 성공 산출물에만 이 marker를 쓴다.
        if (e2e && report.summary.result == BuildResult.Succeeded)
            File.WriteAllText(Path.Combine(outDir, ".aftertaste-e2e-ready"), DateTime.UtcNow.ToString("O"));
    }

    private static void LogSummary(BuildReport report, string outDir)
    {
        var summary = report.summary;
        Debug.Log($"[Build] 결과: {summary.result}, {summary.totalTime.TotalMinutes:F1}분 소요");

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"[Build] 실패. 에러 {summary.totalErrors}, 경고 {summary.totalWarnings}");
            return;
        }

        // 출력 폴더 크기 요약
        long totalRaw = 0, totalBr = 0;
        var lines = new System.Text.StringBuilder();
        lines.AppendLine($"[Build] 산출물 ({outDir}):");
        foreach (var f in Directory.GetFiles(Path.Combine(outDir, "Build")).OrderBy(p => p))
        {
            long size = new FileInfo(f).Length;
            string name = Path.GetFileName(f);
            lines.AppendLine($"  {name}: {size / 1024.0 / 1024.0:F2} MB");
            if (name.EndsWith(".br")) totalBr += size;
            else totalRaw += size;
        }
        long total = totalRaw + totalBr;
        lines.AppendLine($"[Build] Brotli 압축본 합계: {totalBr / 1024.0 / 1024.0:F2} MB");
        lines.AppendLine($"[Build] 전체 산출물: {total / 1024.0 / 1024.0:F2} MB");
        Debug.Log(lines.ToString());
    }
}
