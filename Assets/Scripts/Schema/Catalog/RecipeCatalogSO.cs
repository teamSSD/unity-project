using UnityEngine;

namespace Game.Schema.Catalog
{
    [CreateAssetMenu(menuName = "Game/Catalog/Recipe", fileName = "RecipeCatalog")]
    public class RecipeCatalogSO : CatalogSO<RecipeData>
    {
        protected override string GetKey(RecipeData item) => item.id;
    }
}
