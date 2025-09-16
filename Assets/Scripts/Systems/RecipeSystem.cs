using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RecipeSystem : MonoBehaviour
{
    public List<RecipeData> recipes;

    public RecipeData Search(List<IngredientData> sources)
    {
        var srcSig = sources.Where(i => i != null)
            .Select(i => i.id)
            .OrderBy(id => id)
            .ToArray();

        return recipes.FirstOrDefault(r =>
            r != null && r.inputFoods != null &&
            r.inputFoods.Count == sources.Count &&
            r.inputFoods
                .Where(i => i != null)
                .Select(i => i.id)
                .OrderBy(id => id)
                .SequenceEqual(srcSig));
    }
}
