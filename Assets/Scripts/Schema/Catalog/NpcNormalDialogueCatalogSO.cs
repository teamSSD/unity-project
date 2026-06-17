using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Catalog
{
    /// <summary>
    /// NPC별 일반(Normal) 대사 사이클 카탈로그.
    /// 같은 npcId로 인터랙션할 때마다 sections 배열의 다음 항목을 재생, 끝나면 1로 순환.
    /// 진행 인덱스는 MallPersistent.normalCycleIndices에 저장 (NpcNormalDialogueService 관리).
    /// 퀘스트 단계 대사(DeliveryDialogueConfig.firstMeet 등)와 분리된 시스템.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Catalog/NpcNormalDialogueCatalog", fileName = "NpcNormalDialogueCatalog")]
    public class NpcNormalDialogueCatalogSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string npcId;
            public List<DialogueSO> sections;
        }

        [SerializeField] private List<Entry> entries = new();
        private Dictionary<string, List<DialogueSO>> _byId;

        public List<DialogueSO> GetSections(string npcId)
        {
            if (string.IsNullOrEmpty(npcId)) return null;
            if (_byId == null) RebuildIndex();
            return _byId.TryGetValue(npcId, out var s) ? s : null;
        }

        public bool Has(string npcId) => GetSections(npcId)?.Count > 0;

        private void RebuildIndex()
        {
            _byId = new Dictionary<string, List<DialogueSO>>(entries?.Count ?? 0);
            if (entries == null) return;
            foreach (var e in entries)
                if (!string.IsNullOrEmpty(e.npcId) && e.sections != null && e.sections.Count > 0)
                    _byId[e.npcId] = e.sections;
        }

#if UNITY_EDITOR
        private void OnValidate() => _byId = null;
#endif
    }
}
