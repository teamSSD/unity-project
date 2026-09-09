using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Domain.Cooking
{
    /// <summary>A single real-cooking operation, ordered after all of its dependencies.</summary>
    [Serializable]
    public sealed class RecipeExecutionStep
    {
        public string recipeId;
        public string outputFoodId;
        public string toolId;
        public string minigameId;
        public List<string> inputFoodIds = new();
    }

    [Serializable]
    public sealed class IngredientRequirement
    {
        public string foodId;
        public int quantity;
    }

    /// <summary>
    /// Read-only plan derived from the production recipe catalog. Steps are in post-order:
    /// every processed input is cooked before the step that consumes it.
    /// </summary>
    [Serializable]
    public sealed class RecipeExecutionPlan
    {
        public string targetFoodId;
        public bool valid;
        public string error;
        public List<RecipeExecutionStep> steps = new();
        public List<IngredientRequirement> ingredients = new();
    }

    /// <summary>
    /// Shared recipe-graph resolver used by both headless simulation and the actual-input
    /// WebGL playtest. It observes catalog data only; it never mutates game state.
    /// </summary>
    public sealed class RecipePlanService
    {
        private readonly Dictionary<string, FoodData> _foodById = new();
        private readonly Dictionary<string, RecipeData> _recipeByOutput = new();
        private readonly RecipeLookupService _recipeLookup;

        public RecipePlanService(IEnumerable<FoodData> foods, IEnumerable<RecipeData> recipes)
        {
            foreach (var food in foods ?? Enumerable.Empty<FoodData>())
                if (food != null && !string.IsNullOrWhiteSpace(food.id))
                    _foodById[food.id] = food;

            var recipeList = (recipes ?? Enumerable.Empty<RecipeData>()).Where(recipe => recipe != null).ToList();
            foreach (var recipe in recipeList)
                if (recipe.outputFood != null && !string.IsNullOrWhiteSpace(recipe.outputFood.id))
                    _recipeByOutput[recipe.outputFood.id] = recipe;

            _recipeLookup = new RecipeLookupService(recipeList);
        }

        public RecipeExecutionPlan Build(string targetFoodId)
        {
            var plan = new RecipeExecutionPlan
            {
                targetFoodId = targetFoodId ?? string.Empty,
                valid = true,
                error = string.Empty,
            };
            var ingredientCounts = new Dictionary<string, int>();
            var path = new HashSet<string>();

            if (!Append(targetFoodId, plan.steps, ingredientCounts, path, out var error))
            {
                plan.valid = false;
                plan.error = error;
                plan.steps.Clear();
            }

            plan.ingredients = ingredientCounts
                .OrderBy(pair => pair.Key)
                .Select(pair => new IngredientRequirement { foodId = pair.Key, quantity = pair.Value })
                .ToList();
            return plan;
        }

        private bool Append(
            string foodId,
            List<RecipeExecutionStep> steps,
            Dictionary<string, int> ingredientCounts,
            HashSet<string> path,
            out string error)
        {
            error = string.Empty;
            if (string.IsNullOrWhiteSpace(foodId) || !_foodById.TryGetValue(foodId, out var food))
            {
                error = $"Unknown food '{foodId ?? string.Empty}'.";
                return false;
            }

            if (food.type == FoodType.INGREDIENT)
            {
                ingredientCounts.TryGetValue(foodId, out var count);
                ingredientCounts[foodId] = count + 1;
                return true;
            }

            if (!path.Add(foodId))
            {
                error = $"Recipe cycle detected at '{foodId}'.";
                return false;
            }

            if (!_recipeByOutput.TryGetValue(foodId, out var recipe) || recipe.inputs == null)
            {
                path.Remove(foodId);
                error = $"No recipe produces '{foodId}'.";
                return false;
            }

            foreach (var input in recipe.inputs)
            {
                if (input?.food == null)
                {
                    path.Remove(foodId);
                    error = $"Recipe '{recipe.id}' contains an empty input.";
                    return false;
                }
                if (!Append(input.food.id, steps, ingredientCounts, path, out error))
                {
                    path.Remove(foodId);
                    return false;
                }
            }

            path.Remove(foodId);
            var toolId = _recipeLookup.GetToolIdForMinigame(recipe.minigameId);
            if (string.IsNullOrWhiteSpace(toolId))
            {
                error = $"Recipe '{recipe.id}' has no tool mapping for '{recipe.minigameId}'.";
                return false;
            }

            steps.Add(new RecipeExecutionStep
            {
                recipeId = recipe.id ?? string.Empty,
                outputFoodId = foodId,
                toolId = toolId,
                minigameId = recipe.minigameId ?? string.Empty,
                inputFoodIds = recipe.inputs.Select(input => input.food.id).ToList(),
            });
            return true;
        }
    }
}
