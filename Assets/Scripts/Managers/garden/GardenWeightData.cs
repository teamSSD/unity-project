using System;

public class GardenWeightData : CsvParsable
{
    public string Id { get; private set; }
    public float Weight { get; private set; }

    public void Init(string[] fields)
    {
        if (fields.Length < 9)
        {
            throw new CsvParsingException("Insufficient columns in CSV data.");
        }

        Id = fields[0].Trim();

        if (float.TryParse(fields[8].Trim(), out float parsedWeight))
        {
            Weight = parsedWeight;
        }
        else
        {
            Weight = 0f;
        }
    }
}