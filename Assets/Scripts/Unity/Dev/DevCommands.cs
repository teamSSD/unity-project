using IngameDebugConsole;
using UnityEngine;

/// <summary>
/// IngameDebugConsole (yasirkula) 커맨드. Dev/QA 재현용.
/// 콘솔 열기: 화면 좌상단 popup 클릭 또는 4손가락 탭.
/// [ConsoleMethod] 어트리뷰트는 게임 시작 시 자동 스캔되어 등록됨.
/// </summary>
public static class DevCommands
{
    [ConsoleMethod("phase.set", "현재 페이즈 강제 설정 (Preparation/Morning/Afternoon/Evening/Night). 시간 + TimeManager 자동 동기화.")]
    public static void SetPhase(PhaseType phase)
    {
        var session = GameSessionRoot.Instance;
        var pd = session?.Progress?.PhaseData;
        if (pd == null) { Debug.LogWarning("[DevCommands] PhaseData 없음"); return; }

        pd.Phase = phase;

        // 시간을 페이즈 시작 시각으로 sync (PassPhase의 SetPhaseTime과 동일 로직)
        int hour = phase switch
        {
            PhaseType.Preparation => 5,
            PhaseType.Morning     => 7,
            PhaseType.Afternoon   => 12,
            PhaseType.Evening     => 17,
            PhaseType.Night       => 22,
            _                     => 0,
        };
        session.Stats?.SetTime(hour, 0);

        // Cooking 씬에서 TimeManager가 살아있으면 재초기화 (하드코딩 startHour 무효화)
        TimeManager.Instance?.InitializeTime();

        Debug.Log($"[DevCommands] Phase = {phase}, time = {hour}:00, TimeManager reinit");
    }

    [ConsoleMethod("phase.next", "다음 페이즈로 진행 (PassPhase)")]
    public static void NextPhase()
    {
        var ok = GameSessionRoot.Instance?.Progress?.PassPhase() ?? false;
        Debug.Log($"[DevCommands] PassPhase → {ok}");
    }

    [ConsoleMethod("day.set", "현재 Day 강제 설정")]
    public static void SetDay(int day)
    {
        var pd = GameSessionRoot.Instance?.Progress?.PhaseData;
        if (pd == null) { Debug.LogWarning("[DevCommands] PhaseData 없음"); return; }
        pd.Day = day;
        GameRandom.InitDay(day);
        Debug.Log($"[DevCommands] Day = {day}");
    }

    [ConsoleMethod("save.load", "디스크에서 세이브 즉시 재로드")]
    public static void LoadSave()
    {
        SaveManager.LoadAll();
        Debug.Log("[DevCommands] Save reloaded");
    }

    [ConsoleMethod("save.write", "현재 상태 디스크에 강제 저장")]
    public static void WriteSave()
    {
        SaveManager.SaveAll();
        Debug.Log("[DevCommands] Save written");
    }

    [ConsoleMethod("tutorial.skip", "튜토리얼 강제 완료 처리")]
    public static void SkipTutorial()
    {
        TutorialController.Instance?.Complete();
        Debug.Log("[DevCommands] Tutorial completed");
    }

    [ConsoleMethod("gold.set", "골드 강제 설정")]
    public static void SetGold(int value)
    {
        GameSessionRoot.Instance?.Stats?.SetMoney(value);
        Debug.Log($"[DevCommands] Money = {value}");
    }

    [ConsoleMethod("gold.add", "골드 증감 (음수 가능)")]
    public static void AddGold(int delta)
    {
        GameSessionRoot.Instance?.Stats?.AddMoney(delta);
        Debug.Log($"[DevCommands] Money delta {delta:+#;-#;0}");
    }

    [ConsoleMethod("scene.info", "현재 씬 이름과 페이즈/일차 출력")]
    public static void SceneInfo()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var pd = GameSessionRoot.Instance?.Progress?.PhaseData;
        Debug.Log($"[DevCommands] scene={scene} day={pd?.Day} phase={pd?.Phase} money={GameSessionRoot.Instance?.Stats?.GetMoney()}");
    }
}
