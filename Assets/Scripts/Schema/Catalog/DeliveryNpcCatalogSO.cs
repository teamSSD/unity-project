using UnityEngine;

namespace Game.Schema.Catalog
{
    [CreateAssetMenu(menuName = "Game/Catalog/DeliveryNpc", fileName = "DeliveryNpcCatalog")]
    public class DeliveryNpcCatalogSO : CatalogSO<DeliveryNpcData>
    {
        protected override string GetKey(DeliveryNpcData item) => item.id;

        /// <summary>groupId로 NPC 조회 (id와 별개)</summary>
        public DeliveryNpcData GetByGroupId(string groupId)
        {
            if (string.IsNullOrEmpty(groupId)) return null;
            foreach (var item in items)
            {
                if (item != null && item.groupId == groupId) return item;
            }
            return null;
        }
    }
}
