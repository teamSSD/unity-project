using System.Collections.Generic;

namespace Game.Schema.State
{
    /// <summary>
    /// 튜토리얼 진행 상태 (Set 기반).
    /// shownSteps: 이미 dismiss한 스텝 ID 목록. 임의 순서 허용 — Mall 위치 트리거 등은 접근 순서 예측 불가.
    /// completed: true면 hook 전부 비활성 (이후 실제 게임 흐름).
    /// </summary>
    [System.Serializable]
    public class TutorialState
    {
        public List<int> shownSteps = new List<int>();
        public bool completed;
    }
}
