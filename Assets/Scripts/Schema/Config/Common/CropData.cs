using UnityEngine;

public class CropData : CsvParsable
{
    public string cropId;
    public int    growPhaseCount;
    public float  spawnWeight;
    public string imagePath;
    public Sprite sprite;

    public void Init(string[] f)
    {
        cropId         = f[0].Trim();
        growPhaseCount = int.Parse(f[1].Trim());
        spawnWeight    = float.Parse(f[2].Trim());
        imagePath      = f[3].Trim();
    }
}
