using System.Collections.Generic;
using UnityEngine;

public class CropData : CsvParsable
{
    public string cropId;
    public int    growPhaseCount;
    public float  spawnWeight;
    /// <summary>세미콜론(;) 구분 성장 단계별 sprite path. 씨앗(0) → 성숙(count-1).</summary>
    public List<string> imagePaths = new List<string>();
    /// <summary>성장 단계별 sprite. GameSessionRoot 부팅 시 CropSpriteCatalog에서 채워짐.</summary>
    public Sprite[] sprites = System.Array.Empty<Sprite>();

    public void Init(string[] f)
    {
        cropId         = f[0].Trim();
        growPhaseCount = int.Parse(f[1].Trim());
        spawnWeight    = float.Parse(f[2].Trim());
        string paths   = f[3].Trim();
        imagePaths.Clear();
        foreach (var p in paths.Split(';'))
        {
            var t = p.Trim();
            if (!string.IsNullOrEmpty(t)) imagePaths.Add(t);
        }
    }

    /// <summary>비율 기반 stage 인덱스. currentPhase(0..growPhaseCount) → 0..sprites.Length-1.
    /// 모든 stage가 최소 1회는 등장하도록 floor 매핑.</summary>
    public int GetStageIndex(int currentPhase)
    {
        if (sprites.Length == 0) return 0;
        if (growPhaseCount <= 0) return sprites.Length - 1;
        int idx = currentPhase * sprites.Length / growPhaseCount;
        if (idx < 0) idx = 0;
        if (idx >= sprites.Length) idx = sprites.Length - 1;
        return idx;
    }

    public Sprite GetSpriteAt(int currentPhase)
    {
        if (sprites.Length == 0) return null;
        return sprites[GetStageIndex(currentPhase)];
    }
}
