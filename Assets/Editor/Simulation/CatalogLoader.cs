using System.Collections.Generic;
using Game.Schema.Catalog;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Simulation
{
    /// <summary>Edit-mode에서 프로덕션 카탈로그 SO들을 로드.
    /// CatalogProvider 싱글턴을 우회해 sim harness가 직접 조립할 수 있게.</summary>
    public static class CatalogLoader
    {
        private const string CatalogsRoot = "Assets/Bundles/Catalogs";
        private const string ConfigRoot   = "Assets/Bundles/ScriptableObjects/Config";

        public class Loaded
        {
            public FoodCatalogSO food;
            public RecipeCatalogSO recipe;
            public IngredientCatalogSO ingredient;
            public CookingToolCatalogSO cookingTool;
            public FoodShopConfigSO foodShopConfig;
            public CsvCatalogSO csvs;
        }

        public static Loaded LoadAll()
        {
            return new Loaded
            {
                food = Load<FoodCatalogSO>($"{CatalogsRoot}/FoodCatalog.asset"),
                recipe = Load<RecipeCatalogSO>($"{CatalogsRoot}/RecipeCatalog.asset"),
                ingredient = Load<IngredientCatalogSO>($"{CatalogsRoot}/IngredientCatalog.asset"),
                cookingTool = Load<CookingToolCatalogSO>($"{CatalogsRoot}/CookingToolCatalog.asset"),
                foodShopConfig = Load<FoodShopConfigSO>($"{ConfigRoot}/FoodShopConfig.asset"),
                csvs = Load<CsvCatalogSO>($"{CatalogsRoot}/CsvCatalog.asset"),
            };
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"[Sim] Catalog not found at {path}");
            return asset;
        }

        /// <summary>ingredient id → defaultPrice 매핑. Shop 가격 결정 + 원가 계산용.</summary>
        public static Dictionary<string, int> BuildIngredientPriceMap(IngredientCatalogSO ing)
        {
            var m = new Dictionary<string, int>();
            if (ing?.All == null) return m;
            foreach (var i in ing.All)
            {
                if (i != null && !string.IsNullOrEmpty(i.id)) m[i.id] = i.defaultPrice;
            }
            return m;
        }

        /// <summary>food id → FoodData 매핑.</summary>
        public static Dictionary<string, FoodData> BuildFoodMap(FoodCatalogSO food)
        {
            var m = new Dictionary<string, FoodData>();
            if (food?.All == null) return m;
            foreach (var f in food.All)
            {
                if (f != null && !string.IsNullOrEmpty(f.id)) m[f.id] = f;
            }
            return m;
        }
    }
}
