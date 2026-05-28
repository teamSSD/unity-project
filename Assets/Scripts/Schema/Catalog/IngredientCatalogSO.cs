using UnityEngine;

namespace Game.Schema.Catalog
{
    [CreateAssetMenu(menuName = "Game/Catalog/Ingredient", fileName = "IngredientCatalog")]
    public class IngredientCatalogSO : CatalogSO<IngredientData>
    {
        protected override string GetKey(IngredientData item) => item.id;
    }
}
