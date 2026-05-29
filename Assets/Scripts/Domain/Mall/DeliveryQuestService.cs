using Game.Schema.State.Mall;

namespace Game.Domain.Mall
{
    /// <summary>
    /// NPC 배달 퀘스트 단계 보관/조회 (POCO Service).
    /// MallPersistent에 parallel List로 저장 (디스크 호환).
    /// 단계 전환 로직(accept/cooked 분기)은 호출자(NPC interaction)에 위임 — 이 서비스는 set/get만.
    /// </summary>
    public class DeliveryQuestService
    {
        private readonly MallPersistent _state;

        public DeliveryQuestService(MallPersistent state)
        {
            _state = state;
        }

        public DeliveryQuestStage GetStage(string groupId)
        {
            int idx = _state.questGroupIds.IndexOf(groupId);
            return idx >= 0
                ? (DeliveryQuestStage)_state.questStages[idx]
                : DeliveryQuestStage.FirstMeet;
        }

        public void SetStage(string groupId, DeliveryQuestStage stage)
        {
            int idx = _state.questGroupIds.IndexOf(groupId);
            if (idx >= 0) _state.questStages[idx] = (int)stage;
            else
            {
                _state.questGroupIds.Add(groupId);
                _state.questStages.Add((int)stage);
            }
        }

        public void Clear()
        {
            _state.questGroupIds.Clear();
            _state.questStages.Clear();
        }
    }
}
