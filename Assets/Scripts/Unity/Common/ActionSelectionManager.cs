using UnityEngine;

/// <summary>
/// 액션 선택 데이터 관리 (Singleton)
/// - 아침/점심/저녁별 영업 선택 데이터 저장
/// - DontDestroyOnLoad로 씬 전환 시에도 유지
/// - Pure Data Manager (UI 없음)
/// </summary>
public class ActionSelectionManager : SingletonMonoBehaviour<ActionSelectionManager>
{
    /// <summary>
    /// 액션 선택 데이터 (0=아침, 1=점심, 2=저녁)
    /// </summary>
    private ActionSelection[] actionSelections;

    protected override void OnSingletonAwake()
    {
        InitializeActions();
    }

    private void InitializeActions()
    {
        actionSelections = new ActionSelection[4];
        actionSelections[0] = new ActionSelection("아침");
        actionSelections[1] = new ActionSelection("점심");
        actionSelections[2] = new ActionSelection("저녁");
        actionSelections[3] = new ActionSelection("밤");
    }

    /// <summary>
    /// 액션 선택 데이터 가져오기
    /// </summary>
    public ActionSelection GetAction(int index)
    {
        if (index < 0 || index >= actionSelections.Length)
        {
            Debug.LogError($"[ActionSelectionManager] Invalid action index: {index}");
            return null;
        }
        return actionSelections[index];
    }

    /// <summary>
    /// 액션 설정하기
    /// </summary>
    public void SetAction(int index, ActionType actionType)
    {
        if (index < 0 || index >= actionSelections.Length)
        {
            Debug.LogError($"[ActionSelectionManager] Invalid action index: {index}");
            return;
        }
        actionSelections[index].SelectedAction = actionType;
        Debug.Log($"[ActionSelectionManager] Set {actionSelections[index].Name} action to {actionType}");
    }

    /// <summary>
    /// 모든 액션 선택 초기화
    /// </summary>
    public void ClearAllActions()
    {
        foreach (var action in actionSelections)
        {
            action.Clear();
        }
        Debug.Log("[ActionSelectionManager] All actions cleared");
    }

    /// <summary>
    /// 선택된 액션이 있는지 확인
    /// </summary>
    public bool HasAnySelection()
    {
        foreach (var action in actionSelections)
        {
            if (action.HasSelection())
                return true;
        }
        return false;
    }

}

/// <summary>
/// 액션 선택 데이터 (아침/점심/저녁 각각)
/// </summary>
[System.Serializable]
public class ActionSelection
{
    public string Name;
    public ActionType? SelectedAction;

    public ActionSelection(string name)
    {
        Name = name;
        SelectedAction = null;
    }

    public bool HasSelection()
    {
        return SelectedAction != null;
    }

    public void Clear()
    {
        SelectedAction = null;
    }

    public override string ToString()
    {
        string actionName = SelectedAction?.ToString() ?? "None";
        return $"{Name}: {actionName}";
    }
}
