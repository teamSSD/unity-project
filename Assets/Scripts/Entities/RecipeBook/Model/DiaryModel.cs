using System;
using System.Collections.Generic;
using UnityEngine;

public class DiaryModel
{
    private Dictionary<(PhaseType, ActionType), ActionState> actionStates;
    private PhaseType currentPhase;
    private BentoSelection[] bentoSelections;
    private bool isBentoLocked;

    public event Action<PhaseType, ActionType, ActionState> OnActionStateChanged;
    public event Action<PhaseType> OnPhaseChanged;
    public event Action<ActionType> OnActionExecuted;
    public event Action<string> OnSceneTransitionRequested;
    public event Action<string> OnError;
    public event Action<int, FoodData> OnBentoFoodAdded;
    public event Action<int, FoodData> OnBentoFoodRemoved;
    public event Action<bool> OnBentoLockedChanged;

    private readonly IPhaseProgressor phaseProgressor;
    private readonly Dictionary<ActionType, IActionCommand> actionCommands;

    public DiaryModel(IPhaseProgressor phaseProgressor)
    {
        this.phaseProgressor = phaseProgressor ?? throw new ArgumentNullException(nameof(phaseProgressor));

        actionStates = new Dictionary<(PhaseType, ActionType), ActionState>();
        currentPhase = PhaseType.Preparation;
        isBentoLocked = false;

        bentoSelections = new BentoSelection[3];
        for (int i = 0; i < 3; i++)
        {
            bentoSelections[i] = new BentoSelection($"도시락 {i + 1}", i + 1);
        }

        actionCommands = new Dictionary<ActionType, IActionCommand>
        {
            { ActionType.MenuSelect, new MenuSelectAction() },
            { ActionType.PrepareIngredients, new PrepareIngredientsAction() },
            { ActionType.Work, new WorkAction() },
            { ActionType.Rest, new RestAction() },
            { ActionType.Shopping, new ShoppingAction() }
        };
    }

    public void RegisterAction(PhaseType phase, ActionType actionType)
    {
        if (actionType == ActionType.None) return;
        if (!ActionDatabase.Exists(actionType)) return;

        var key = (phase, actionType);
        if (!actionStates.ContainsKey(key))
        {
            var info = ActionDatabase.GetInfo(actionType);
            actionStates[key] = info.InitialState;
        }
    }

    public ActionState GetState(PhaseType phase, ActionType actionType)
    {
        return actionStates.TryGetValue((phase, actionType), out var state)
            ? state
            : ActionState.Disavailable;
    }

    public bool SetState(PhaseType phase, ActionType actionType, ActionState newState)
    {
        var key = (phase, actionType);
        var oldState = GetState(phase, actionType);
        if (oldState == newState) return false;

        actionStates[key] = newState;
        OnActionStateChanged?.Invoke(phase, actionType, newState);

        Debug.Log($"[DiaryModel] [{phase}] {actionType}: {oldState} → {newState}");
        return true;
    }

    public void EnableAction(PhaseType phase, ActionType actionType)
    {
        SetState(phase, actionType, ActionState.Available);
    }

    public void DisableAction(PhaseType phase, ActionType actionType)
    {
        SetState(phase, actionType, ActionState.Disavailable);
    }

    public bool ExecuteAction(PhaseType phase, ActionType actionType)
    {
        Debug.Log($"[DiaryModel] ExecuteAction: {actionType} in {phase}");

        var state = GetState(phase, actionType);
        if (state != ActionState.Available)
        {
            Debug.LogWarning($"[DiaryModel] Cannot execute {actionType} - state is {state}");
            return false;
        }

        var rule = PhaseCompletionConfig.GetRule(phase);
        if (rule.Mode == CompletionMode.SINGLE)
        {
            var selectedAction = GetSelectedAction(phase);
            if (selectedAction.HasValue && selectedAction.Value != actionType)
            {
                CancelSelection(phase, selectedAction.Value);
            }
        }

        if (phase > PhaseType.Preparation)
        {
            PhaseType prevPhase = phase - 1;
            if (!IsPhaseCompleted(prevPhase))
            {
                Debug.LogWarning($"[DiaryModel] Cannot execute {actionType} - previous phase {prevPhase} not completed");
                return false;
            }
        }

        if (!actionCommands.TryGetValue(actionType, out var command))
        {
            OnError?.Invoke($"Unknown action: {actionType}");
            return false;
        }

        var result = command.Execute(phase, this);

        if (result.Success)
        {
            SetState(phase, actionType, result.NewState);
            OnActionExecuted?.Invoke(actionType);

            if (IsPhaseCompleted(phase))
            {
                Debug.Log($"[DiaryModel] Phase {phase} completed! Moving to next phase.");
                phaseProgressor.PassPhase();
            }
        }
        else if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            OnError?.Invoke(result.ErrorMessage);
        }

        return result.Success;
    }

    private ActionType? GetSelectedAction(PhaseType phase)
    {
        foreach (var kvp in actionStates)
        {
            if (kvp.Key.Item1 == phase && kvp.Value == ActionState.Selected)
            {
                return kvp.Key.Item2;
            }
        }
        return null;
    }

    private void CancelSelection(PhaseType phase, ActionType actionType)
    {
        SetState(phase, actionType, ActionState.Available);
    }

    public bool IsPhaseCompleted(PhaseType phase)
    {
        var rule = PhaseCompletionConfig.GetRule(phase);
        var phaseActions = GetPhaseActions(phase);

        switch (rule.Mode)
        {
            case CompletionMode.ALL:
                foreach (var requiredAction in rule.RequiredActions)
                {
                    if (GetState(phase, requiredAction) != ActionState.Done)
                        return false;
                }
                return true;

            case CompletionMode.SINGLE:
                foreach (var kvp in phaseActions)
                {
                    if (kvp.Value == ActionState.Done || kvp.Value == ActionState.Selected)
                        return true;
                }
                return false;

            default:
                return false;
        }
    }

    private Dictionary<ActionType, ActionState> GetPhaseActions(PhaseType phase)
    {
        var result = new Dictionary<ActionType, ActionState>();
        foreach (var kvp in actionStates)
        {
            if (kvp.Key.Item1 == phase)
            {
                result[kvp.Key.Item2] = kvp.Value;
            }
        }
        return result;
    }

    public void SyncPhase(PhaseType newPhase)
    {
        if (currentPhase == newPhase) return;

        currentPhase = newPhase;
        Debug.Log($"[DiaryModel] Phase synced to {currentPhase}");

        if (newPhase == PhaseType.Preparation)
        {
            UnlockBentoSelections();
            Debug.Log("[DiaryModel] Bentos unlocked for new Preparation phase");
        }

        MarkPreviousPhasesAsCompleted(newPhase);
        RefreshPhaseActions();
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private void MarkPreviousPhasesAsCompleted(PhaseType currentPhase)
    {
        for (PhaseType phase = PhaseType.Preparation; phase < currentPhase; phase++)
        {
            var rule = PhaseCompletionConfig.GetRule(phase);

            if (rule.Mode == CompletionMode.ALL)
            {
                foreach (var actionType in rule.RequiredActions)
                {
                    var state = GetState(phase, actionType);
                    if (state != ActionState.Disavailable)
                    {
                        SetState(phase, actionType, ActionState.Done);
                    }
                }
            }
            else
            {
                foreach (var actionType in new[] { ActionType.Work, ActionType.Rest, ActionType.Shopping })
                {
                    var state = GetState(phase, actionType);
                    if (state != ActionState.Disavailable)
                    {
                        SetState(phase, actionType, ActionState.Done);
                    }
                }
            }
        }
    }

    private void RefreshPhaseActions()
    {
        Debug.Log($"[DiaryModel] RefreshPhaseActions for {currentPhase}");

        DisableAllAvailableActions();

        switch (currentPhase)
        {
            case PhaseType.Preparation:
                EnableAction(PhaseType.Preparation, ActionType.PrepareIngredients);
                break;

            case PhaseType.Morning:
            case PhaseType.Afternoon:
            case PhaseType.Evening:
            case PhaseType.Night:
                EnableAction(currentPhase, ActionType.Work);
                EnableAction(currentPhase, ActionType.Rest);
                EnableAction(currentPhase, ActionType.Shopping);
                break;
        }
    }

    public void DisableAllAvailableActions()
    {
        var keys = new List<(PhaseType, ActionType)>(actionStates.Keys);
        foreach (var key in keys)
        {
            if (actionStates[key] == ActionState.Available)
            {
                SetState(key.Item1, key.Item2, ActionState.Disavailable);
            }
        }
    }

    public bool AddBentoFood(FoodData food)
    {
        if (isBentoLocked)
        {
            OnError?.Invoke("도시락이 잠겨있습니다. (MenuSelect 이미 완료)");
            return false;
        }

        for (int i = 0; i < bentoSelections.Length; i++)
        {
            var bento = bentoSelections[i];

            if (food.type == FoodType.MAIN && bento.MainMenu == null)
            {
                bento.MainMenu = food;
                OnBentoFoodAdded?.Invoke(i, food);
                UpdateMenuSelectState();
                return true;
            }

            if (food.type == FoodType.SIDE && bento.MainMenu != null && bento.SideMenus.Count < 3)
            {
                if (bento.SideMenus.Contains(food))
                    continue;

                bento.SideMenus.Add(food);
                OnBentoFoodAdded?.Invoke(i, food);
                return true;
            }
        }

        if (food.type == FoodType.MAIN)
        {
            OnError?.Invoke("메인 메뉴를 추가할 빈 도시락이 없습니다. (최대 3개)");
        }
        else
        {
            OnError?.Invoke("사이드 메뉴를 추가할 공간이 없습니다.");
        }
        return false;
    }

    public bool AddBentoFood(int bentoIndex, FoodData food)
    {
        if (bentoIndex < 0 || bentoIndex >= bentoSelections.Length)
            return false;

        if (isBentoLocked)
        {
            OnError?.Invoke("도시락이 잠겨있습니다. (MenuSelect 이미 완료)");
            return false;
        }

        var bento = bentoSelections[bentoIndex];

        if (food.type == FoodType.MAIN)
        {
            if (bento.MainMenu != null)
            {
                OnError?.Invoke($"도시락 {bentoIndex + 1}번에 이미 메인 메뉴가 있습니다.");
                return false;
            }
            bento.MainMenu = food;
        }
        else if (food.type == FoodType.SIDE)
        {
            if (bento.MainMenu == null)
            {
                OnError?.Invoke("메인 메뉴를 먼저 추가해주세요.");
                return false;
            }

            if (bento.SideMenus.Count >= 3)
            {
                OnError?.Invoke("사이드 메뉴는 최대 3개까지만 추가할 수 있습니다.");
                return false;
            }

            if (bento.SideMenus.Contains(food))
            {
                OnError?.Invoke("이미 추가된 사이드 메뉴입니다.");
                return false;
            }

            bento.SideMenus.Add(food);
        }
        else
        {
            return false;
        }

        OnBentoFoodAdded?.Invoke(bentoIndex, food);
        UpdateMenuSelectState();
        return true;
    }

    public bool RemoveBentoFood(FoodData food)
    {
        if (isBentoLocked)
        {
            OnError?.Invoke("도시락이 잠겨있습니다. (MenuSelect 이미 완료)");
            return false;
        }

        for (int i = 0; i < bentoSelections.Length; i++)
        {
            var bento = bentoSelections[i];

            if (food.type == FoodType.MAIN && bento.MainMenu == food)
            {
                bento.MainMenu = null;
                OnBentoFoodRemoved?.Invoke(i, food);
                UpdateMenuSelectState();
                return true;
            }

            if (food.type == FoodType.SIDE && bento.SideMenus.Remove(food))
            {
                OnBentoFoodRemoved?.Invoke(i, food);
                return true;
            }
        }

        Debug.LogWarning($"[DiaryModel] Food not found in any bento: {food.ingredientName}");
        return false;
    }

    public bool RemoveBentoFood(int bentoIndex, FoodData food)
    {
        if (bentoIndex < 0 || bentoIndex >= bentoSelections.Length)
            return false;

        if (isBentoLocked)
        {
            OnError?.Invoke("도시락이 잠겨있습니다. (MenuSelect 이미 완료)");
            return false;
        }

        var bento = bentoSelections[bentoIndex];
        bool removed = false;

        if (food.type == FoodType.MAIN && bento.MainMenu == food)
        {
            bento.MainMenu = null;
            removed = true;
        }
        else if (food.type == FoodType.SIDE)
        {
            removed = bento.SideMenus.Remove(food);
        }

        if (!removed)
            return false;

        OnBentoFoodRemoved?.Invoke(bentoIndex, food);
        UpdateMenuSelectState();
        return true;
    }

    public BentoSelection GetBentoSelection(int index)
    {
        if (index < 0 || index >= bentoSelections.Length)
            return null;
        return bentoSelections[index];
    }

    public int GetBentoCount()
    {
        return bentoSelections.Length;
    }

    public BentoSelection GetBentoForDisplay(int index)
    {
        return GetBentoSelection(index);
    }

    public bool HasAnyBentoSelection()
    {
        foreach (var bento in bentoSelections)
        {
            if (bento.HasSelection())
                return true;
        }
        return false;
    }

    public string ValidateBentoSelections()
    {
        int selectedCount = 0;
        foreach (var bento in bentoSelections)
        {
            if (bento.HasSelection())
            {
                selectedCount++;
                if (!bento.IsValid())
                    return $"{bento.Name}이(가) 유효하지 않습니다.";
            }
        }

        if (selectedCount < 1)
            return "최소 1개의 도시락을 선택해야 합니다.";
        if (selectedCount > 3)
            return "최대 3개의 도시락까지만 선택할 수 있습니다.";

        return null;
    }

    public void LockBentoSelections()
    {
        if (isBentoLocked) return;

        isBentoLocked = true;
        OnBentoLockedChanged?.Invoke(true);
    }

    public void UnlockBentoSelections()
    {
        if (!isBentoLocked) return;

        isBentoLocked = false;
        OnBentoLockedChanged?.Invoke(false);
    }

    public string GetBentoSummary()
    {
        var summary = "";
        foreach (var bento in bentoSelections)
        {
            if (bento.HasSelection())
                summary += bento.ToString() + "\n";
        }
        return summary;
    }

    private void UpdateMenuSelectState()
    {
        var currentState = GetState(PhaseType.Preparation, ActionType.MenuSelect);
        if (currentState == ActionState.Done)
            return;

        if (HasAnyBentoSelection())
        {
            EnableAction(PhaseType.Preparation, ActionType.MenuSelect);
        }
        else
        {
            DisableAction(PhaseType.Preparation, ActionType.MenuSelect);
        }
    }

    public void EnableMenuSelect()
    {
        UpdateMenuSelectState();
    }

    public void DisableMenuSelect()
    {
        DisableAction(PhaseType.Preparation, ActionType.MenuSelect);
    }

    public void RequestSceneTransition(string sceneName)
    {
        OnSceneTransitionRequested?.Invoke(sceneName);
    }

    public DiaryStateSnapshot CreateSnapshot()
    {
        var snapshot = new DiaryStateSnapshot
        {
            CurrentPhase = currentPhase,
            ActionStates = new Dictionary<(PhaseType, ActionType), ActionState>(actionStates),
            BentoSelections = new BentoSelection[3]
        };

        for (int i = 0; i < 3; i++)
        {
            var original = bentoSelections[i];
            snapshot.BentoSelections[i] = new BentoSelection(original.Name, original.OrderNumber)
            {
                MainMenu = original.MainMenu,
                SideMenus = new List<FoodData>(original.SideMenus),
                AllowDuplicates = original.AllowDuplicates
            };
        }

        return snapshot;
    }

    public void RestoreSnapshot(DiaryStateSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.IsValid())
        {
            OnError?.Invoke("Invalid snapshot");
            return;
        }

        currentPhase = snapshot.CurrentPhase;
        actionStates = new Dictionary<(PhaseType, ActionType), ActionState>(snapshot.ActionStates);

        for (int i = 0; i < 3; i++)
        {
            var source = snapshot.BentoSelections[i];
            bentoSelections[i] = new BentoSelection(source.Name, source.OrderNumber)
            {
                MainMenu = source.MainMenu,
                SideMenus = new List<FoodData>(source.SideMenus),
                AllowDuplicates = source.AllowDuplicates
            };
        }

        OnPhaseChanged?.Invoke(currentPhase);
    }

    public PhaseType CurrentPhase => currentPhase;
    public int ActionCount => actionStates.Count;
    public bool IsBentoLocked => isBentoLocked;
}
