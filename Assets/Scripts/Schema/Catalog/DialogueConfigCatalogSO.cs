using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Schema.Catalog
{
    /// <summary>
    /// groupId → DeliveryDialogueConfig 매핑.
    /// DeliveryDialogueConfig는 자체 id 필드가 없으므로 Entry로 명시 매핑.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Catalog/DialogueConfig", fileName = "DialogueConfigCatalog")]
    public class DialogueConfigCatalogSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string groupId;
            public DeliveryDialogueConfig config;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();
        private Dictionary<string, DeliveryDialogueConfig> _byGroupId;

        public DeliveryDialogueConfig GetByGroupId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;
            if (_byGroupId == null) RebuildIndex();
            return _byGroupId.TryGetValue(groupId, out var c) ? c : null;
        }

        public int Count => entries?.Count ?? 0;

        private void RebuildIndex()
        {
            _byGroupId = new Dictionary<string, DeliveryDialogueConfig>(entries?.Count ?? 0);
            if (entries == null) return;
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.groupId) && e.config != null)
                    _byGroupId[e.groupId] = e.config;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _byGroupId = null;
        }
#endif
    }
}
