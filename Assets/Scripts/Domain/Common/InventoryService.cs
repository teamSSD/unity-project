using System.Collections.Generic;
using System.Linq;
using Game.Domain.Shop;
using UnityEngine;

namespace Game.Domain.Common
{
    /// <summary>
    /// 인벤토리 로직 (POCO Service).
    /// 런타임 상태: Dictionary&lt;FoodData, List&lt;InventoryBatch&gt;&gt;
    /// 디스크 형식: InventorySaveData (foodId 기반 string ID, 카탈로그 lookup으로 복원).
    /// 의존: 카탈로그(FoodData 리스트), StorageUpgradeService(CanAcceptType 한정).
    /// </summary>
    public class InventoryService : LoadInventoryUsecase
    {
        // 시작 메뉴 원재료
        // 시작 재료는 인벤토리 lv 0 capacity (Refrigerator 7 / UpperShelf 3 / LowerShelf 4) 안에 맞춤.
        // 시작 unlock 3개 메뉴 (I044 기계장 고기정식 / I060 옥상 오믈렛 / I046 루미 젤리) input만 포함.
        private static readonly Dictionary<string, int> StartingIngredients = new()
        {
            { "I007", 3 }, { "I008", 3 }, { "I009", 3 }, { "I010", 3 },
            { "I017", 3 }, { "I019", 3 }, { "I020", 3 },
            { "I026", 3 }, { "I027", 3 },
        };

        private readonly Dictionary<FoodData, List<InventoryBatch>> _inventory = new();
        private readonly List<FoodData> _allFoodData;
        private readonly StorageUpgradeService _storage;

        public InventoryService(IEnumerable<FoodData> catalog, StorageUpgradeService storage)
        {
            _allFoodData = catalog != null ? new List<FoodData>(catalog) : new List<FoodData>();
            _storage = storage;
        }

        public void ResetToDefault()
        {
            _inventory.Clear();
            foreach (var food in _allFoodData)
            {
                if (food.type == FoodType.INGREDIENT && StartingIngredients.TryGetValue(food.id, out int qty))
                {
                    int expDays = GetExpirationDays(food);
                    _inventory[food] = new List<InventoryBatch> {
                        new InventoryBatch { quantity = qty, daysRemaining = expDays }
                    };
                }
            }
        }

        public void AddFood(FoodData food, int amount)
        {
            if (food == null || amount <= 0) return;
            int expDays = GetExpirationDays(food);
            if (!_inventory.ContainsKey(food)) _inventory[food] = new List<InventoryBatch>();
            var batches = _inventory[food];

            // 같은 daysRemaining인 배치가 있으면 병합 (구매 시점 달라도 같은 유통기한이면 한 슬롯).
            var existing = batches.Find(b => b.daysRemaining == expDays);
            if (existing != null)
            {
                existing.quantity += amount;
            }
            else
            {
                batches.Add(new InventoryBatch { quantity = amount, daysRemaining = expDays });
                SortBatches(batches);
            }
        }

        public void ConsumeFood(FoodData food, int amount)
        {
            if (food == null || amount <= 0) return;
            if (!_inventory.TryGetValue(food, out var batches)) return;

            int remaining = amount;
            for (int i = 0; i < batches.Count && remaining > 0; i++)
            {
                int take = Mathf.Min(remaining, batches[i].quantity);
                batches[i].quantity -= take;
                remaining -= take;
            }
            batches.RemoveAll(b => b.quantity <= 0);
            if (batches.Count == 0) _inventory.Remove(food);
        }

        public int CheckStockAmount(FoodData food)
        {
            if (food == null || !_inventory.TryGetValue(food, out var batches)) return 0;
            int total = 0;
            foreach (var b in batches) total += b.quantity;
            return total;
        }

        public List<InventoryBatch> GetBatches(FoodData food)
        {
            if (food == null || !_inventory.TryGetValue(food, out var batches))
                return new List<InventoryBatch>();
            return new List<InventoryBatch>(batches);
        }

        /// <summary>특정 배치를 통째로 폐기 (환불 없음). 인벤토리 UI 버리기 버튼에서 호출.</summary>
        public bool DiscardBatch(FoodData food, InventoryBatch batch)
        {
            if (food == null || batch == null) return false;
            if (!_inventory.TryGetValue(food, out var batches)) return false;
            if (!batches.Remove(batch)) return false;
            if (batches.Count == 0) _inventory.Remove(food);
            return true;
        }

        public List<(FoodData food, IngredientData ingredient)> LoadIngredientsByCategory(IngredientDisplayCategory category)
        {
            return _inventory
                .Where(kv => kv.Value.Count > 0 && kv.Key.ingredient != null && kv.Key.ingredient.display == category)
                .Select(kv => (kv.Key, kv.Key.ingredient))
                .ToList();
        }

        public bool CanAcceptType(FoodData food)
        {
            if (food == null) return false;
            if (food.ingredient == null) return true;
            if (_inventory.TryGetValue(food, out var batches) && batches.Count > 0) return true;

            string upgradeType = UpgradeTypeFromCategory(food.ingredient.display);
            if (string.IsNullOrEmpty(upgradeType)) return true;

            int max = _storage != null
                ? _storage.GetCurrentData(upgradeType)?.value ?? int.MaxValue
                : int.MaxValue;
            int unique = LoadIngredientsByCategory(food.ingredient.display).Count;
            return unique < max;
        }

        public void AddHarvestedCrop(string cropId, int amount)
        {
            FoodData food = _allFoodData?.Find(f => f.id == cropId);
            if (food != null) AddFood(food, amount);
        }

        public void AdvanceDay()
        {
            var emptyKeys = new List<FoodData>();
            foreach (var kv in _inventory)
            {
                foreach (var batch in kv.Value) batch.daysRemaining--;
                kv.Value.RemoveAll(b => b.daysRemaining <= 0);
                if (kv.Value.Count == 0) emptyKeys.Add(kv.Key);
            }
            foreach (var key in emptyKeys) _inventory.Remove(key);
        }

        // ── 저장/로드 ──

        public InventorySaveData GetSaveData()
        {
            var data = new InventorySaveData();
            foreach (var kv in _inventory)
            {
                if (kv.Value.Count == 0) continue;
                var entry = new InventoryItemEntry { foodId = kv.Key.id };
                foreach (var batch in kv.Value)
                {
                    entry.batches.Add(new InventoryBatchEntry
                    {
                        quantity = batch.quantity,
                        daysRemaining = batch.daysRemaining
                    });
                }
                data.items.Add(entry);
            }
            return data;
        }

        public void ApplySaveData(InventorySaveData data)
        {
            _inventory.Clear();
            if (data.items != null && data.items.Count > 0)
            {
                foreach (var entry in data.items)
                {
                    FoodData food = _allFoodData.Find(f => f.id == entry.foodId);
                    if (food == null) continue;
                    var batches = new List<InventoryBatch>();
                    foreach (var be in entry.batches)
                        batches.Add(new InventoryBatch { quantity = be.quantity, daysRemaining = be.daysRemaining });
                    SortBatches(batches);
                    _inventory[food] = batches;
                }
            }
            // 레거시 폴백 (foodIds + amounts → 단일 배치)
            else if (data.foodIds != null && data.foodIds.Count > 0)
            {
                for (int i = 0; i < data.foodIds.Count; i++)
                {
                    FoodData food = _allFoodData.Find(f => f.id == data.foodIds[i]);
                    if (food == null) continue;
                    int expDays = GetExpirationDays(food);
                    _inventory[food] = new List<InventoryBatch> {
                        new InventoryBatch { quantity = data.amounts[i], daysRemaining = expDays }
                    };
                }
            }
        }

        // ── 유틸 ──

        private static int GetExpirationDays(FoodData food)
        {
            if (food.ingredient != null && food.ingredient.expirationDay > 0)
                return food.ingredient.expirationDay;
            return 10;
        }

        private static void SortBatches(List<InventoryBatch> batches)
        {
            batches.Sort((a, b) => a.daysRemaining.CompareTo(b.daysRemaining));
        }

        private static string UpgradeTypeFromCategory(IngredientDisplayCategory c) => c switch
        {
            IngredientDisplayCategory.UpperShelf   => "upperShelf",
            IngredientDisplayCategory.LowerShelf   => "lowerShelf",
            IngredientDisplayCategory.Refrigerator => "refrigerator",
            _ => null,
        };
    }
}
