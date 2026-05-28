using System.Collections.Generic;
using System.Linq;

public class MenuSchema
{
    public string name;
    public int orderNumber;
    public List<FoodData> mainMenus;
    public List<FoodData> sideMenus;

    public FoodData mainMenu => mainMenus?.FirstOrDefault();

    public MenuSchema(string name, int orderNumber, FoodData mainMenu, List<FoodData> sideMenus)
        : this(name, orderNumber, mainMenu != null ? new List<FoodData> { mainMenu } : new List<FoodData>(), sideMenus) { }

    public MenuSchema(string name, int orderNumber, List<FoodData> mainMenus, List<FoodData> sideMenus)
    {
        this.name = name;
        this.orderNumber = orderNumber;
        this.mainMenus = mainMenus ?? new List<FoodData>();
        this.sideMenus = sideMenus ?? new List<FoodData>();
    }

    public override string ToString()
    {
        string mainNames = string.Join("+", mainMenus?.Select(f => f.ingredientName) ?? new string[0]);
        string sideNames = string.Join(", ", sideMenus?.Select(f => f.ingredientName) ?? new string[0]);
        return $"{name} (#{orderNumber}): Main={mainNames}, Sides=[{sideNames}]";
    }
}