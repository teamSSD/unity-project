using System;
using System.Collections.Generic;

namespace Game.Schema.State.Mall
{
    /// <summary>
    /// Mall 도메인 디스크 직렬화 대상.
    /// - NPC별 DeliveryQuestStage 진행도 (groupId → stage int)
    /// - NPC별 일반 대사 사이클 인덱스 (npcId → 다음 재생할 section index)
    /// parallel List 형태로 SaveData(groupIds/stages, npcIds/indices)와 동일 shape.
    /// </summary>
    [Serializable]
    public class MallPersistent
    {
        public List<string> questGroupIds = new();
        public List<int>    questStages   = new();

        public List<string> normalCycleNpcIds  = new();
        public List<int>    normalCycleIndices = new();
    }
}
