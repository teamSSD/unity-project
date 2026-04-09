using System.Collections.Generic;
using System.Linq;

public class MenuSchema
{
    public string name;
    public int orderNumber;
    public FoodData mainMenu;
    public List<FoodData> sideMenus;

    public MenuSchema(string name, int orderNumber, FoodData mainMenu, List<FoodData> sideMenus)
    {
        this.name = name;
        this.orderNumber = orderNumber;
        this.mainMenu = mainMenu;
        this.sideMenus = sideMenus;
    }

    public override string ToString()
    {
        string mainName = mainMenu?.ingredientName ?? "None";
        string sideNames = string.Join(", ", sideMenus?.Select(f => f.ingredientName) ?? new string[0]);
        return $"{name} (#{orderNumber}): Main={mainName}, Sides=[{sideNames}]";
    }
}