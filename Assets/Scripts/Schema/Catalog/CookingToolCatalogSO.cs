using UnityEngine;

namespace Game.Schema.Catalog
{
    [CreateAssetMenu(menuName = "Game/Catalog/CookingTool", fileName = "CookingToolCatalog")]
    public class CookingToolCatalogSO : CatalogSO<CookingToolData>
    {
        protected override string GetKey(CookingToolData item) => item.id;
    }
}
