using UnityEngine;

namespace Game.Schema.Catalog
{
    [CreateAssetMenu(menuName = "Game/Catalog/DeliveryNpc", fileName = "DeliveryNpcCatalog")]
    public class DeliveryNpcCatalogSO : CatalogSO<DeliveryNpcData>
    {
        protected override string GetKey(DeliveryNpcData item) => item.id;

        /// <summary>groupId로 NPC 조회 (id와 별개) — 그룹에 여러 NPC가 있으면 첫 번째</summary>
        public DeliveryNpcData GetByGroupId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;
            foreach (var item in items)
            {
                if (item != null && item.groupId == groupId) return item;
            }
            return null;
        }

        /// <summary>groupId의 모든 멤버 NPC id 목록 (페어/그룹 quest용)</summary>
        public System.Collections.Generic.IEnumerable<string> GetAllIdsByGroupId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) yield break;
            foreach (var item in items)
            {
                if (item != null && item.groupId == groupId) yield return item.id;
            }
        }
    }
}
