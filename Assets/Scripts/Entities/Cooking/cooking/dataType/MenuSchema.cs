using System.Collections.Generic;

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
}