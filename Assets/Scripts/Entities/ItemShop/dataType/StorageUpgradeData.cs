public class StorageUpgradeData : CsvParsable
{
    public string type;
    public int level;
    public int cost;
    public int value;

    public void Init(string[] f)
    {
        type  = f[0].Trim();
        level = int.Parse(f[1].Trim());
        cost  = int.Parse(f[2].Trim());
        value = int.Parse(f[3].Trim());
    }
}
