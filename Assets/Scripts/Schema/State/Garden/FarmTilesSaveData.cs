using System;

/// <summary>
/// 농장 전체 타일 배열의 디스크 직렬화 형식. SaveManager.GameSaveData.farmTiles 슬롯.
/// GardenPersistent.tiles와 SaveManager에서 변환.
/// </summary>
[Serializable]
public class FarmTilesSaveData
{
    public FarmTileSaveData[] tiles = new FarmTileSaveData[8];
}
