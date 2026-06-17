using System.Collections.Generic;

/// <summary>
/// NPC 일반 대사 사이클 진행도 디스크 직렬화 형식. MallPersistent와 SaveManager에서 변환.
/// npcIds[i] ↔ indices[i] (현재 다음 재생 인덱스).
/// </summary>
[System.Serializable]
public class NpcNormalCycleSaveData
{
    public List<string> npcIds  = new();
    public List<int>    indices = new();
}
