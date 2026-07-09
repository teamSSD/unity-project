using System.Linq;
using Game.Editor.Simulation.Events;

namespace Game.Editor.Simulation
{
    /// <summary>Farm 자동 수확/심기 시뮬. 매 페이즈 진입 시 tick.
    /// - 잠긴 타일 (tile 업그레이드 미달) 스킵
    /// - 심긴 타일: 성장 완료면 수확 (inventory에 추가) + 자동 재심기
    /// - 빈 타일: 자동 심기 (CropCatalog weighted random)</summary>
    public static class FarmSim
    {
        public static void Tick(SimContext ctx)
        {
            var persistent = ctx.State.garden.persistent;
            if (persistent?.tiles == null) return;

            int unlockedTiles = (int)(ctx.FarmUpgrade.GetCurrentData("tile")?.value ?? 3f);
            float timeReduction = ctx.FarmUpgrade.GetCurrentData("timeReduction")?.value ?? 0f;
            int harvestQty = (int)(ctx.FarmUpgrade.GetCurrentData("harvestCount")?.value ?? 5f);

            for (int i = 0; i < persistent.tiles.Length && i < unlockedTiles; i++)
            {
                var tile = persistent.tiles[i];
                if (tile == null)
                {
                    tile = new FarmTileSaveData();
                    persistent.tiles[i] = tile;
                }

                // 심겨 있으면 수확 가능한지 체크
                if (!string.IsNullOrEmpty(tile.cropId))
                {
                    var crop = ctx.CropCatalog?.GetCropById(tile.cropId);
                    if (crop == null) { tile.cropId = null; continue; }

                    int passed = ctx.Progress.CurrentPhaseIndex - tile.plantedPhase;
                    int required = UnityEngine.Mathf.CeilToInt(crop.growPhaseCount * (1f - timeReduction));
                    if (passed >= required)
                    {
                        // 수확 → inventory에 추가
                        var food = ctx.FoodById.Values.FirstOrDefault(f => f?.id == crop.cropId);
                        if (food != null)
                        {
                            ctx.Inventory.AddFood(food, harvestQty);
                            ctx.Log.Add(new FarmEvent
                            {
                                day = ctx.State.phase.Day,
                                phase = (int)ctx.State.phase.Phase,
                                action = "harvest",
                                cropId = crop.cropId,
                                tileIndex = i,
                                yieldQty = harvestQty,
                            });
                        }
                        tile.cropId = null; // 자동 심기 아래에서 처리
                    }
                }

                // 비었으면 자동 심기 (weighted random)
                if (string.IsNullOrEmpty(tile.cropId))
                {
                    var crop = ctx.CropCatalog?.GetRandomCropByWeight();
                    if (crop != null)
                    {
                        tile.cropId = crop.cropId;
                        tile.plantedPhase = ctx.Progress.CurrentPhaseIndex;
                        ctx.Log.Add(new FarmEvent
                        {
                            day = ctx.State.phase.Day,
                            phase = (int)ctx.State.phase.Phase,
                            action = "plant",
                            cropId = crop.cropId,
                            tileIndex = i,
                            yieldQty = 0,
                        });
                    }
                }
            }
        }
    }
}
