using NUnit.Framework;
using UnityEditor;

public class CookingAssetConsistencyTest
{
    [Test]
    public void CookingAssets_FollowCsvRecipeAndSpriteContracts()
    {
        var violations = IngredientImageConsistencyChecker.CollectViolations();

        Assert.That(
            violations,
            Is.Empty,
            "Cooking asset contracts must stay synchronized.\n" + string.Join("\n", violations));
    }

    [Test]
    public void CriticalBowlIngredients_UseDistinctSemanticAssets()
    {
        var blackSesame = AssetDatabase.LoadAssetAtPath<FoodData>(
            "Assets/Bundles/ScriptableObjects/FoodData/I024.asset");
        var cheongyangLeaf = AssetDatabase.LoadAssetAtPath<FoodData>(
            "Assets/Bundles/ScriptableObjects/FoodData/I025.asset");

        Assert.That(blackSesame, Is.Not.Null);
        Assert.That(cheongyangLeaf, Is.Not.Null);
        Assert.That(blackSesame.toolVariants[2], Is.Not.Null);
        Assert.That(cheongyangLeaf.toolVariants[2], Is.Not.Null);
        Assert.That(
            AssetDatabase.GetAssetPath(blackSesame.toolVariants[2]),
            Does.EndWith("/item_blackSesame_bowl.png"));
        Assert.That(
            AssetDatabase.GetAssetPath(cheongyangLeaf.toolVariants[2]),
            Does.EndWith("/item_cheongyangLeaf_bowl.png"));
        Assert.That(blackSesame.toolVariants[2], Is.Not.SameAs(cheongyangLeaf.toolVariants[2]));
    }

    [TestCase("I023")]
    [TestCase("I024")]
    [TestCase("I025")]
    public void LegacyPanIngredients_RemainBowlOnly(string foodId)
    {
        var food = AssetDatabase.LoadAssetAtPath<FoodData>(
            $"Assets/Bundles/ScriptableObjects/FoodData/{foodId}.asset");

        Assert.That(food, Is.Not.Null);
        Assert.That(food.availableTools, Is.EquivalentTo(new[] { "T003" }));
    }
}
