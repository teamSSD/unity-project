using UnityEngine;

[CreateAssetMenu(fileName = "CropData", menuName = "Scriptable Objects/CropData")]
public class CropData : ScriptableObject
{
    public string cropId;
    public int growPhaseCount;
    public int harvestCount;
    public int seedReturnCount;
}
