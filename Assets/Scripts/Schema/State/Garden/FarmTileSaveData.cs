using System;

/// <summary>
/// 단일 농장 타일의 직렬화 상태 (작물 ID + 심은 페이즈).
/// 디스크 JSON 호환을 위해 전역 namespace 유지.
/// </summary>
[Serializable]
public class FarmTileSaveData
{
    public string cropId = "";
    public int plantedPhase;
}
