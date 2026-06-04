using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MenuValidator.CalculateReward 기획 공식 (Aftertaste_Book.md L153-158):
///   보상 = 총 가격 × 메인 배율 × 사이드 배율
///   메인 배율: 일치 1.0 / 불일치 0.7
///   사이드 배율: 1.0 + (일치 사이드 × 0.05)
///   총 가격 = 메인 + 사이드들 가격 단순 합 (개별 페널티 없음)
/// </summary>
public class MenuValidatorPriceTest
{
    private FoodData CreateFoodData(string id, string name, FoodType type = FoodType.MAIN)
    {
        var foodData = ScriptableObject.CreateInstance<FoodData>();
        foodData.id = id;
        foodData.ingredientName = name;
        foodData.type = type;
        foodData.availableTools = new List<string>();
        return foodData;
    }

    private FoodSchema CreateFoodSchema(FoodData foodData, int price)
    {
        return new FoodSchema(foodData, price);
    }

    private MenuSchema CreateMenuSchema(FoodData mainMenu, List<FoodData> sideMenus)
    {
        return new MenuSchema("테스트 메뉴", 1, mainMenu, sideMenus);
    }

    /// <summary>
    /// 완벽 매치 (메인 1000, 사이드 300+400 모두 일치)
    /// 총가격 1700 × 메인 1.0 × 사이드 1.10 = 1870
    /// </summary>
    [Test]
    public void CalculateReward_PerfectMatch_Returns1870()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC });
        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema> {
            CreateFoodSchema(sideB, 300),
            CreateFoodSchema(sideC, 400)
        };

        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);
        Assert.AreEqual(1870, reward);
    }

    /// <summary>
    /// 메인 일치, 사이드 1/2 일치
    /// 총가격 1800 × 메인 1.0 × 사이드 1.05 = 1890
    /// </summary>
    [Test]
    public void CalculateReward_MainMatchOneSideWrong_Returns1890()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var sideX = CreateFoodData("I099", "사이드X", FoodType.SIDE);
        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC });
        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema> {
            CreateFoodSchema(sideB, 300),
            CreateFoodSchema(sideX, 500)
        };

        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);
        Assert.AreEqual(1890, reward);
    }

    /// <summary>
    /// 메인 불일치, 사이드 1/1 일치
    /// 총가격 1800 × 메인 0.7 × 사이드 1.05 = 1323
    /// </summary>
    [Test]
    public void CalculateReward_MainWrong_Returns1323()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var mainX = CreateFoodData("I099", "메인X", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB });
        var providedMain = CreateFoodSchema(mainX, 1500);
        var providedSides = new List<FoodSchema> {
            CreateFoodSchema(sideB, 300)
        };

        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);
        Assert.AreEqual(1323, reward);
    }

    /// <summary>
    /// 메인만 (사이드 없는 주문)
    /// 총가격 2000 × 메인 1.0 × 사이드 1.0 = 2000
    /// </summary>
    [Test]
    public void CalculateReward_MainOnlyPerfect_Returns2000()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var order = CreateMenuSchema(mainA, new List<FoodData>());
        var providedMain = CreateFoodSchema(mainA, 2000);

        int reward = MenuValidator.CalculateReward(order, providedMain, new List<FoodSchema>());
        Assert.AreEqual(2000, reward);
    }

    /// <summary>
    /// 메인 일치, 사이드 모두 틀림
    /// 총가격 1800 × 메인 1.0 × 사이드 1.0 = 1800
    /// </summary>
    [Test]
    public void CalculateReward_MainMatchAllSidesWrong_Returns1800()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var sideX = CreateFoodData("I098", "사이드X", FoodType.SIDE);
        var sideY = CreateFoodData("I099", "사이드Y", FoodType.SIDE);
        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC });
        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema> {
            CreateFoodSchema(sideX, 400),
            CreateFoodSchema(sideY, 400)
        };

        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);
        Assert.AreEqual(1800, reward);
    }

    /// <summary>
    /// 메인 일치 + 주문보다 사이드 1개 더 (추가 X)
    /// 총가격 1500 × 메인 1.0 × 사이드 1.05 = 1575
    /// </summary>
    [Test]
    public void CalculateReward_ExtraSide_Returns1575()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideX = CreateFoodData("I099", "사이드X", FoodType.SIDE);
        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB });
        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema> {
            CreateFoodSchema(sideB, 300),
            CreateFoodSchema(sideX, 200)
        };

        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);
        Assert.AreEqual(1575, reward);
    }

    [Test]
    public void CalculateReward_NullOrder_Returns0()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var providedMain = CreateFoodSchema(mainA, 1000);
        int reward = MenuValidator.CalculateReward(null, providedMain, null);
        Assert.AreEqual(0, reward);
    }

    [Test]
    public void CalculateReward_NullProvidedMain_Returns0()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var order = CreateMenuSchema(mainA, new List<FoodData>());
        int reward = MenuValidator.CalculateReward(order, null, null);
        Assert.AreEqual(0, reward);
    }

    /// <summary>
    /// 사이드 3개 모두 일치
    /// 총가격 1600 × 메인 1.0 × 사이드 1.15 = 1840
    /// </summary>
    [Test]
    public void CalculateReward_ThreeSidesPerfect_Returns1840()
    {
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var sideD = CreateFoodData("I004", "사이드D", FoodType.SIDE);
        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC, sideD });
        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema> {
            CreateFoodSchema(sideB, 200),
            CreateFoodSchema(sideC, 200),
            CreateFoodSchema(sideD, 200)
        };

        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);
        Assert.AreEqual(1840, reward);
    }
}
