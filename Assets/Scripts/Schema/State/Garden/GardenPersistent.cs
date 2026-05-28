using System;

namespace Game.Schema.State.Garden
{
    /// <summary>
    /// Garden 도메인 디스크 직렬화 대상. Phase 3-C-1 진행하며 필드 추가됨:
    /// - upgradeLevels (FarmUpgradeManager 흡수)
    /// - tiles (FarmTileStorage 흡수)
    /// </summary>
    [Serializable]
    public class GardenPersistent
    {
    }
}
