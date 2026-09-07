using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Development WebGL에서만 브라우저 E2E 실행기를 위한 관측/제어 경계.
/// 게임 조작을 대신하지 않고, 상태 관측과 테스트 메타데이터만 제공한다.
/// </summary>
#if AFTERTASTE_E2E
public sealed class AftertasteE2ETestBridge : MonoBehaviour
{
    private const string ObjectName = "AftertasteE2ETestBridge";
    private static bool _created;

    [Serializable]
    private sealed class Command
    {
        public string action;
        public string label;
        public float value;
    }

    [Serializable]
    private sealed class Snapshot
    {
        public string type;
        public string label;
        public string scene;
        public string phase;
        public int day;
        public int hour;
        public int minute;
        public int money;
        public int stamina;
        public bool uiLocked;
        public float realtime;
        public int frame;
        public float timeScale;
    }

    [Serializable]
    private sealed class LogEvent
    {
        public string type;
        public string level;
        public string message;
        public string scene;
        public float realtime;
        public int frame;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if (_created) return;
        _created = true;

        var bridge = new GameObject(ObjectName).AddComponent<AftertasteE2ETestBridge>();
        DontDestroyOnLoad(bridge.gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        Application.logMessageReceived += OnLog;
        EmitSnapshot("bridge-ready");
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        Application.logMessageReceived -= OnLog;
    }

    /// <summary>WebGL의 window.AftertasteE2E.command가 SendMessage로 호출한다.</summary>
    public void ReceiveCommand(string json)
    {
        var command = JsonUtility.FromJson<Command>(json);
        if (command == null || string.IsNullOrWhiteSpace(command.action))
        {
            EmitLog("warning", "Invalid E2E command");
            return;
        }

        switch (command.action)
        {
            case "snapshot":
                EmitSnapshot(command.label);
                break;
            case "mark":
                EmitSnapshot(string.IsNullOrWhiteSpace(command.label) ? "marker" : command.label);
                break;
            case "timeScale":
                Time.timeScale = Mathf.Clamp(command.value, 0f, 20f);
                EmitSnapshot("time-scale-changed");
                break;
            default:
                EmitLog("warning", $"Unsupported E2E command: {command.action}");
                break;
        }
    }

    private void OnSceneLoaded(Scene _, LoadSceneMode __) => EmitSnapshot("scene-loaded");

    private void OnLog(string message, string _, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        EmitJson(JsonUtility.ToJson(new LogEvent
        {
            type = "unity-log",
            level = type.ToString(),
            message = message,
            scene = SceneManager.GetActiveScene().name,
            realtime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
        }));
    }

    private void EmitSnapshot(string label)
    {
        var state = GameStateReporter.CaptureRuntimeState();
        EmitJson(JsonUtility.ToJson(new Snapshot
        {
            type = "snapshot",
            label = label ?? string.Empty,
            scene = SceneManager.GetActiveScene().name,
            phase = state.Phase ?? string.Empty,
            day = state.Day,
            hour = state.Hour,
            minute = state.Minute,
            money = state.Money,
            stamina = state.Stamina,
            uiLocked = UILockManager.IsLocked,
            realtime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
            timeScale = Time.timeScale,
        }));
    }

    private void EmitLog(string level, string message)
    {
        EmitJson(JsonUtility.ToJson(new LogEvent
        {
            type = "bridge-log",
            level = level,
            message = message,
            scene = SceneManager.GetActiveScene().name,
            realtime = Time.realtimeSinceStartup,
            frame = Time.frameCount,
        }));
    }

    private static void EmitJson(string json)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        AftertasteE2EEmit(json);
#else
        Debug.Log($"[AftertasteE2E] {json}");
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void AftertasteE2EEmit(string json);
#endif
}
#endif
