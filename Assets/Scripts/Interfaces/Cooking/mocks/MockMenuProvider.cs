using System.Collections.Generic;
using UnityEngine;

class MockMenuProvider : ISelectMenu
{
    public List<MenuSchema> GetTodaysMenu()
    {
        return new List<MenuSchema>
        {
            new MenuSchema(
                "도시락 정식 A",
                -1,
                Resources.Load<FoodData>("ScriptableObjects/FoodData/I034"),
                new List<FoodData>
                {
                    Resources.Load<FoodData>("ScriptableObjects/FoodData/I046"),
                    Resources.Load<FoodData>("ScriptableObjects/FoodData/I058"),
                    Resources.Load<FoodData>("ScriptableObjects/FoodData/I062")
                }
            ),
        };
    }
}