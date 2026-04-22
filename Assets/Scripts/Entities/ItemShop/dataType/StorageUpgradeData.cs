public class StorageUpgradeData : CsvParsable
{
    public int level;
    public int cost;
    public int refrigeratorCapacity;
    public int upperShelfCapacity;
    public int lowerShelfCapacity;

    public void Init(string[] f)
    {
        level = int.Parse(f[0].Trim());
        cost = int.Parse(f[1].Trim());
        refrigeratorCapacity = int.Parse(f[2].Trim());
        upperShelfCapacity = int.Parse(f[3].Trim());
        lowerShelfCapacity = int.Parse(f[4].Trim());
    }
}
