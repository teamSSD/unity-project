/// <summary>
/// 씬 전환 간 FarmTile 상태를 보관하는 정적 저장소.
/// SaveManager와 연동하여 디스크 저장/로드도 지원.
/// </summary>
public static class FarmTileStorage
{
    private const int MaxTiles = 8;
    private static readonly FarmTileSaveData[] tiles = new FarmTileSaveData[MaxTiles];

    public static FarmTileSaveData GetTileData(int farmIndex)
    {
        if (farmIndex < 0 || farmIndex >= MaxTiles) return null;
        return tiles[farmIndex];
    }

    public static void SetTileData(int farmIndex, FarmTileSaveData data)
    {
        if (farmIndex < 0 || farmIndex >= MaxTiles) return;
        tiles[farmIndex] = data;
    }

    public static FarmTilesSaveData GetSaveData()
    {
        return new FarmTilesSaveData { tiles = (FarmTileSaveData[])tiles.Clone() };
    }

    public static void ApplySaveData(FarmTilesSaveData data)
    {
        for (int i = 0; i < MaxTiles; i++)
            tiles[i] = (data?.tiles != null && i < data.tiles.Length) ? data.tiles[i] : null;
    }

    public static void Clear()
    {
        for (int i = 0; i < MaxTiles; i++) tiles[i] = null;
    }
}

[System.Serializable]
public class FarmTilesSaveData
{
    public FarmTileSaveData[] tiles = new FarmTileSaveData[8];
}
