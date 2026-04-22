public class ToolUpgradeData : CsvParsable
{
    public string toolId;
    public int level;
    public int cost;
    public int staminaCost;
    public float durationMultiplier;

    public void Init(string[] f)
    {
        toolId = f[0].Trim();
        level = int.Parse(f[1].Trim());
        cost = int.Parse(f[2].Trim());
        staminaCost = int.Parse(f[3].Trim());
        durationMultiplier = float.Parse(f[4].Trim());
    }
}
