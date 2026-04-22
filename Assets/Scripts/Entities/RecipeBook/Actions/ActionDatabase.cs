using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 액션 정보를 관리하는 정적 데이터베이스
/// ScriptableObject 없이 모든 액션 데이터를 코드로 관리합니다.
/// </summary>
public static class ActionDatabase
{
    private static readonly Dictionary<ActionType, ActionInfo> _actions = new()
    {
        // ========================================
        // 액션 선택 UI에서 사용 (아침/점심/저녁)
        // ========================================
        {
            ActionType.Work,
            new ActionInfo(
                displayName: "영업",
                description: "가게를 엽니다",
                initialState: ActionState.Available,
                targetSceneName: "Cooking",
                commandId: ""
            )
        },
        {
            ActionType.Rest,
            new ActionInfo(
                displayName: "휴식",
                description: "스테미너를 충전합니다",
                initialState: ActionState.Available,
                targetSceneName: "",
                commandId: ""
            )
        },
        {
            ActionType.Shopping,
            new ActionInfo(
                displayName: "상가 이동",
                description: "상가로 이동합니다",
                initialState: ActionState.Available,
                targetSceneName: SceneNames.Shop,
                commandId: ""
            )
        }
    };

    /// <summary>
    /// 액션 타입에 해당하는 정보 가져오기
    /// </summary>
    public static ActionInfo GetInfo(ActionType type)
    {
        if (_actions.TryGetValue(type, out var info))
        {
            return info;
        }

        Debug.LogError($"[ActionDatabase] ActionType.{type} not found in database!");
        return default;
    }

    /// <summary>
    /// 액션 타입이 데이터베이스에 존재하는지 확인
    /// </summary>
    public static bool Exists(ActionType type)
    {
        return type != ActionType.None && _actions.ContainsKey(type);
    }

    /// <summary>
    /// 모든 액션 타입 가져오기
    /// </summary>
    public static IEnumerable<ActionType> GetAllActionTypes()
    {
        return _actions.Keys;
    }
}

/// <summary>
/// 액션 정보 구조체
/// </summary>
public struct ActionInfo
{
    public string DisplayName;
    public string Description;
    public Sprite Icon;
    public ActionState InitialState;
    public string TargetSceneName;
    public string CommandId;

    public ActionInfo(string displayName, string description, ActionState initialState, string targetSceneName = "", string commandId = "", Sprite icon = null)
    {
        DisplayName = displayName;
        Description = description;
        Icon = icon;
        InitialState = initialState;
        TargetSceneName = targetSceneName;
        CommandId = commandId;
    }
}
