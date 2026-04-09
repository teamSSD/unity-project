using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Diary 전체 상태 스냅샷
/// Schema 레이어: 저장/로드/테스트용 데이터 구조
/// </summary>
[Serializable]
public class DiaryStateSnapshot
{
    public PhaseType CurrentPhase;
    public Dictionary<(PhaseType, ActionType), ActionState> ActionStates;
    public BentoSelection[] BentoSelections;

    public DiaryStateSnapshot()
    {
        CurrentPhase = PhaseType.Morning;
        ActionStates = new Dictionary<(PhaseType, ActionType), ActionState>();
        BentoSelections = new BentoSelection[3];

        // 빈 도시락 초기화
        for (int i = 0; i < 3; i++)
        {
            BentoSelections[i] = new BentoSelection($"도시락 {i + 1}", i + 1);
        }
    }

    /// <summary>
    /// 스냅샷 전체 유효성 검증
    /// </summary>
    public bool IsValid()
    {
        // 도시락 검증
        if (BentoSelections == null || BentoSelections.Length != 3)
            return false;

        foreach (var bento in BentoSelections)
        {
            if (bento == null || !bento.IsValid())
                return false;
        }

        // 액션 상태 검증
        if (ActionStates == null)
            return false;

        // 최소 1개 도시락 선택 필요 (기획 요구사항)
        int selectedCount = BentoSelections.Count(b => b.HasSelection());
        if (selectedCount < 1 || selectedCount > 3)
            return false;

        return true;
    }

    /// <summary>
    /// 깊은 복사 생성
    /// </summary>
    public DiaryStateSnapshot Clone()
    {
        var clone = new DiaryStateSnapshot
        {
            CurrentPhase = this.CurrentPhase,
            ActionStates = new Dictionary<(PhaseType, ActionType), ActionState>(this.ActionStates)
        };

        // 도시락 깊은 복사
        for (int i = 0; i < 3; i++)
        {
            var original = BentoSelections[i];
            clone.BentoSelections[i] = new BentoSelection(original.Name, original.OrderNumber)
            {
                MainMenu = original.MainMenu,
                SideMenus = new List<FoodData>(original.SideMenus),
                AllowDuplicates = original.AllowDuplicates
            };
        }

        return clone;
    }

    public override string ToString()
    {
        var bentoSummary = string.Join("\n", BentoSelections.Select(b => b.ToString()));
        return $"[DiaryStateSnapshot]\nPhase: {CurrentPhase}\nActions: {ActionStates.Count}\nBentos:\n{bentoSummary}";
    }
}
