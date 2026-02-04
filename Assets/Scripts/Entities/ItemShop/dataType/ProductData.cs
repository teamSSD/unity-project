using System;
using System.Collections.Generic;
using System.Linq;

public enum ProductType
{
    General, Special
}

public class ProductData : CsvParsable
{
    public string id;
    public ProductType type;

    public void Init(string[] args)
    {
        if (args.Length != 2) throw new CsvParsingException("length of args isn't match.");
        try
        {
            id = args[0].Trim();
            type = (ProductType)Enum.Parse(typeof(ProductType), args[1].Trim());
        }
        catch (Exception e)
        {
            throw new CsvParsingException($"Exception occured during parsing csv\nmessage : {e.Message}");
        }
    }

    public ProductData() { }

    public ProductData(string id)
    {
        this.id = id;
    }
}