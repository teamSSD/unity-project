using UnityEngine;

namespace Game.Schema.Catalog
{
    [CreateAssetMenu(menuName = "Game/Catalog/Food", fileName = "FoodCatalog")]
    public class FoodCatalogSO : CatalogSO<FoodData>
    {
        protected override string GetKey(FoodData item) => item.id;
    }
}
