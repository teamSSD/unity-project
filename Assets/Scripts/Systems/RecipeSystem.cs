using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class RecipeSystem : MonoBehaviour
{
    /*
    public List<RecipeData> recipes;

    public RecipeData Search(List<IngredientData> sources, int cookingToolId)
    {
        var srcSig = sources.Where(i => i != null)
            .Select(i => i.id)
            .OrderBy(id => id)
            .ToArray();
        
        return recipes.Where(recipe => recipe.cookingToolId == cookingToolId)
            .FirstOrDefault(r =>
                r != null && r.inputFoodIds != null &&
                r.inputFoodIds.Count == sources.Count &&
                r.inputFoodIds
                    .OrderBy(id => id)
                    .SequenceEqual(srcSig));
    }
    */
}
