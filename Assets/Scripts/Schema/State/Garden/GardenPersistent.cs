using System;
using System.Collections.Generic;

namespace Game.Schema.State.Garden
{
    /// <summary>
    /// Garden 도메인 디스크 직렬화 대상.
    /// - upgradeTypes/upgradeLevels: FarmUpgradeManager.levels 흡수 (디스크 호환 위해 parallel List)
    /// - tiles: FarmTileStorage 흡수 (Phase 3-C-1-d)
    /// </summary>
    [Serializable]
    public class GardenPersistent
    {
        public List<string> upgradeTypes  = new();
        public List<int>    upgradeLevels = new();
    }
}
