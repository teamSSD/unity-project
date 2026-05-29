using System;
using System.Collections.Generic;

namespace Game.Schema.State.Shop
{
    /// <summary>
    /// Shop 도메인 디스크 직렬화 대상.
    /// Storage/Tool 업그레이드 레벨 흡수 (parallel List, 디스크 호환 유지).
    /// </summary>
    [Serializable]
    public class ShopPersistent
    {
        public List<string> storageTypes  = new();
        public List<int>    storageLevels = new();

        public List<string> toolIds      = new();
        public List<int>    toolLevels   = new();
    }
}
