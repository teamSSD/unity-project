using System.Collections.Generic;

/// <summary>
/// Garden 도메인 ↔ GameSaveData 슬롯 변환 (디스크 호환 유지).
/// GardenPersistent(upgradeTypes/upgradeLevels/tiles) ↔ FarmUpgradeSaveData + FarmTilesSaveData.
/// </summary>
public static class GardenSaveAdapter
{
    public static void Capture(GameSaveData save)
    {
        if (GameSessionRoot.Instance == null) return;
        var gp = GameSessionRoot.Instance.State.garden.persistent;

        save.farmUpgrades = new FarmUpgradeSaveData
        {
            types  = new List<string>(gp.upgradeTypes),
            levels = new List<int>(gp.upgradeLevels)
        };
        save.farmTiles = new FarmTilesSaveData
        {
            tiles = (FarmTileSaveData[])gp.tiles.Clone()
        };
    }

    public static void Apply(GameSaveData save)
    {
        if (GameSessionRoot.Instance == null) return;
        var gp = GameSessionRoot.Instance.State.garden.persistent;

        // 업그레이드: Service 초기화 시 채워진 type 유지하며 saved level만 덮어쓰기
        if (save.farmUpgrades != null)
        {
            for (int i = 0; i < save.farmUpgrades.types.Count; i++)
            {
                string type = save.farmUpgrades.types[i];
                int level = save.farmUpgrades.levels[i];
                int idx = gp.upgradeTypes.IndexOf(type);
                if (idx >= 0) gp.upgradeLevels[idx] = level;
                else
                {
                    gp.upgradeTypes.Add(type);
                    gp.upgradeLevels.Add(level);
                }
            }
        }

        // 타일: saved와 정확히 일치하게 복원
        for (int i = 0; i < gp.tiles.Length; i++)
            gp.tiles[i] = (save.farmTiles?.tiles != null && i < save.farmTiles.tiles.Length)
                ? save.farmTiles.tiles[i] : null;
    }
}
