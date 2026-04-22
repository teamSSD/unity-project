public class FarmUpgradeData : CsvParsable
{
    public int level;
    public int cost;
    public int tileCount;
    public float timeReduction;
    public int harvestCount;

    public void Init(string[] f)
    {
        level         = int.Parse(f[0].Trim());
        cost          = int.Parse(f[1].Trim());
        tileCount     = int.Parse(f[2].Trim());
        timeReduction = float.Parse(f[3].Trim());
        harvestCount  = int.Parse(f[4].Trim());
    }
}
