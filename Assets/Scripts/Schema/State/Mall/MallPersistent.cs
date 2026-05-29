using System;
using System.Collections.Generic;

namespace Game.Schema.State.Mall
{
    /// <summary>
    /// Mall 도메인 디스크 직렬화 대상.
    /// - NPC별 DeliveryQuestStage 진행도 (groupId → stage int)
    /// parallel List 형태로 DeliveryQuestSaveData(groupIds/stages)와 동일 shape.
    /// </summary>
    [Serializable]
    public class MallPersistent
    {
        public List<string> questGroupIds = new();
        public List<int>    questStages   = new();
    }
}
