using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Application.logMessageReceived 훅으로 Error/Exception 발생 시 게임 상태 스냅샷 자동 dump.
/// 로그가 stack trace만 나오고 어떤 상태였는지 몰라서 디버깅 못 하는 문제 해결용.
/// 무한 재귀 방지 + 30초 dedup으로 스팸 방지.</summary>
public static class GameStateReporter
{
    private static bool _dumping;
    private static float _lastDumpAt;
    private static string _lastDumpKey;
    private static readonly Queue<string> _recentScenes = new(4);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Debug.Log는 stack trace 없이 1줄로 표시 (콘솔 볼륨 15배 감소).
        // Warning/Error/Exception은 stack trace 유지 (디버깅 필요).
        Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
        Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Exception, StackTraceLogType.ScriptOnly);
        Application.SetStackTraceLogType(LogType.Assert, StackTraceLogType.ScriptOnly);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _recentScenes.Enqueue(scene.name);
        while (_recentScenes.Count > 4) _recentScenes.Dequeue();
    }

    // 이 문자열이 message 앞에 있으면 dump 안 함 (진단/알림용이라 state 컨텍스트 불필요).
    private static readonly string[] IgnorePrefixes =
    {
        "[TextureDiag]", "[FontPreWarmer]", "[LogSpam]",
    };

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (_dumping) return;

        // Error/Exception/Assert 만 dump 대상. Warning은 스팸 위험 커서 제외.
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;

        // 특정 prefix로 시작하는 로그는 dump 스킵.
        if (message != null)
            foreach (var p in IgnorePrefixes)
                if (message.StartsWith(p)) return;

        // 같은 에러가 매 프레임 발생하는 경우 30초 내 재로그 skip.
        string key = message?.Length > 100 ? message.Substring(0, 100) : message;
        if (key == _lastDumpKey && Time.realtimeSinceStartup - _lastDumpAt < 30f) return;
        _lastDumpKey = key;
        _lastDumpAt = Time.realtimeSinceStartup;

        _dumping = true;
        try
        {
            Debug.Log(BuildSnapshot(type, message));
        }
        catch { /* 스냅샷 자체가 실패해도 원 에러 방해 안 함 */ }
        finally { _dumping = false; }
    }

    private static string BuildSnapshot(LogType type, string trigger)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"[GameState @ {type}] trigger=\"{Trim(trigger, 80)}\"");
        sb.AppendLine($"  Scene: active={SceneManager.GetActiveScene().name}, recent=[{string.Join("→", _recentScenes)}]");
        sb.AppendLine($"  Time: realtime={Time.realtimeSinceStartup:F1}s, frame={Time.frameCount}, timeScale={Time.timeScale}");

        var session = GameSessionRoot.Instance;
        if (session == null) { sb.AppendLine("  Session: null"); return sb.ToString().TrimEnd(); }

        var ps = session.Progress;
        var pd = ps?.PhaseData;
        if (pd != null)
            sb.AppendLine($"  Progress: Day={pd.Day}, Phase={pd.Phase}");
        else
            sb.AppendLine("  Progress: null");

        var ss = session.Stats;
        if (ss != null)
            sb.AppendLine($"  Stats: money={ss.GetMoney()}, stamina={ss.GetStamina()}, time={ss.GetHour():D2}:{ss.GetMinute():D2}");

        sb.AppendLine($"  UILocked: {UILockManager.IsLocked}");

        // Audio 관련 상태 (audio listener warning 원인 추적).
        int listenerCount = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length;
        var enabledSources = new List<string>();
        foreach (var src in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            if (src.isPlaying) enabledSources.Add($"{src.gameObject.name}(clip={src.clip?.name ?? "null"})");
        sb.AppendLine($"  Audio: listeners={listenerCount}, playing=[{string.Join(",", enabledSources)}]");

        try
        {
            var inv = session.Inventory?.GetSaveData();
            if (inv != null)
                sb.AppendLine($"  Inventory: items={inv.items.Count}");
        }
        catch { /* Inventory 접근 실패 시 무시 */ }

        return sb.ToString().TrimEnd();
    }

    private static string Trim(string s, int max)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Length <= max ? s : s.Substring(0, max) + "…";
    }
}
