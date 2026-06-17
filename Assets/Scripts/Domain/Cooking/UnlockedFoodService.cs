using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Game.Domain.Cooking
{
    /// <summary>
    /// 레시피 해금 상태 (POCO Service). UnlockedFoodManager facade 후속.
    /// IUnlockedFoodProvider 구현 + Save/Load (gamedata.json unlockedRecipes 슬롯).
    /// </summary>
    public class UnlockedFoodService : IUnlockedFoodProvider
    {
        private HashSet<string> unlockedRecipeIds = new HashSet<string>();
        private readonly List<FoodData> allFoodData;

        public UnlockedFoodService(IEnumerable<FoodData> catalog)
        {
            allFoodData = catalog != null ? new List<FoodData>(catalog) : new List<FoodData>();
        }

        public UnlockedRecipesSaveData GetSaveData()
        {
            return new UnlockedRecipesSaveData { recipeIds = unlockedRecipeIds.ToList() };
        }

        public void ApplySaveData(UnlockedRecipesSaveData data)
        {
            if (data != null && data.recipeIds != null && data.recipeIds.Count > 0)
                unlockedRecipeIds = new HashSet<string>(data.recipeIds);
            else
                UnlockDefaultRecipes();
        }

        /// <summary>Legacy fallback: PhaseData.UnlockedRecipes(piggyback) 폐기 전 데이터 호환.</summary>
        public void LoadUnlocksFromProgressLegacy(PhaseData pd)
        {
            var unlockedList = pd?.UnlockedRecipes;
            if (unlockedList != null && unlockedList.Count > 0)
                unlockedRecipeIds = new HashSet<string>(unlockedList);
            else
                UnlockDefaultRecipes();
        }

        public void UnlockDefaultRecipes()
        {
            // I062 환기구 연어구이는 quest 경로에 없어 default 유지 필수.
            // 시작 재료는 환기구 raw 빠짐 (UpperShelf cap fit) — 게임 중 shop 구매로 조리 가능.
            string[] defaultMains = { "I044", "I060" };
            string[] defaultSides = { "I046", "I062" };
            foreach (var id in defaultMains) UnlockRecipe(id);
            foreach (var id in defaultSides) UnlockRecipe(id);
        }

        public void UnlockRecipe(string foodId)
        {
            if (unlockedRecipeIds.Add(foodId))
                Debug.Log($"[UnlockedFoodService] Unlocked recipe: {foodId}");
        }

        public void LockRecipe(string foodId)
        {
            if (unlockedRecipeIds.Remove(foodId))
                Debug.Log($"[UnlockedFoodService] Locked recipe: {foodId}");
        }

        public void UnlockAll()
        {
            unlockedRecipeIds.Clear();
            foreach (var food in allFoodData) unlockedRecipeIds.Add(food.id);
        }

        public List<FoodData> GetAllMainFoods() =>
            allFoodData.Where(f => f.type == FoodType.MAIN).OrderBy(f => f.id).ToList();

        public List<FoodData> GetAllSideFoods() =>
            allFoodData.Where(f => f.type == FoodType.SIDE).OrderBy(f => f.id).ToList();

        public List<FoodData> GetUnlockedMainFoods() =>
            allFoodData.Where(f => f.type == FoodType.MAIN && unlockedRecipeIds.Contains(f.id)).ToList();

        public List<FoodData> GetUnlockedSideFoods() =>
            allFoodData.Where(f => f.type == FoodType.SIDE && unlockedRecipeIds.Contains(f.id)).ToList();

        public bool IsUnlocked(string foodId) => unlockedRecipeIds.Contains(foodId);
    }
}
