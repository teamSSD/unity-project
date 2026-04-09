using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Scriptable Objects/CropData")]
public class CropData : ScriptableObject
{
    public string cropId;
    public int growPhaseCount;
    public int harvestCount;
    public int seedReturnCount;

    [Header("시각적 요소")]
    [Tooltip("성장 단계별 이미지 (0: 씨앗, 마지막: 수확 가능)")]
    public Sprite[] growthSprites;
}
