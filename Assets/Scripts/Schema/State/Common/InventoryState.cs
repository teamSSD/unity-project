using System.Collections.Generic;

namespace Game.Schema.State
{
    /// <summary>
    /// 세션 동안 유지되는 인벤토리의 유일한 런타임 상태.
    /// 영속화할 때는 InventorySaveData snapshot으로 변환한다.
    /// </summary>
    public sealed class InventoryState
    {
        public Dictionary<FoodData, List<InventoryBatch>> BatchesByFood { get; } = new();
    }
}
