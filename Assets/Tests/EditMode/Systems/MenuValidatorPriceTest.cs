using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MenuValidator의 가격 계산 로직 테스트
/// 산식 검증:
/// - 메뉴당 배율: 주문 포함 100%, 미포함 70%
/// - 기본 배율: 100% + (일치 사이드 개수 × 10%)
/// - 추가 배율: 모두일치 130%, 메인일치 100%, 메인불일치 70%
/// </summary>
public class MenuValidatorPriceTest
{
    // Helper: FoodData 생성
    private FoodData CreateFoodData(string id, string name, FoodType type = FoodType.MAIN)
    {
        var foodData = ScriptableObject.CreateInstance<FoodData>();
        foodData.id = id;
        foodData.ingredientName = name;
        foodData.type = type;
        foodData.availableTools = new List<string>();
        return foodData;
    }

    // Helper: FoodSchema 생성
    private FoodSchema CreateFoodSchema(FoodData foodData, int price)
    {
        return new FoodSchema(foodData, price);
    }

    // Helper: MenuSchema 생성
    private MenuSchema CreateMenuSchema(FoodData mainMenu, List<FoodData> sideMenus)
    {
        return new MenuSchema("테스트 메뉴", 1, mainMenu, sideMenus);
    }

    /// <summary>
    /// 테스트 1: 완벽한 주문 (모두 일치)
    /// 주문: 메인A(1000원), 사이드B(300원), 사이드C(400원)
    /// 제공: 메인A(1000원), 사이드B(300원), 사이드C(400원)
    ///
    /// 계산:
    /// 1. 각 음식: 1000 + 300 + 400 = 1700원
    /// 2. 기본 배율: 1700 × (100% + 2×10%) = 1700 × 1.2 = 2040원
    /// 3. 추가 배율: 2040 × 1.3 = 2652원
    /// </summary>
    [Test]
    public void CalculateReward_PerfectMatch_Returns2652()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);

        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC });

        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema>
        {
            CreateFoodSchema(sideB, 300),
            CreateFoodSchema(sideC, 400)
        };

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(2652, reward, "완벽한 주문 시 보상은 2652원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 2: 사이드 1개 틀림 (메인 일치)
    /// 주문: 메인A(1000원), 사이드B(300원), 사이드C(400원)
    /// 제공: 메인A(1000원), 사이드B(300원), 사이드X(500원)
    ///
    /// 계산:
    /// 1. 각 음식: 1000 + 300 + (500×0.7) = 1650원
    /// 2. 기본 배율: 1650 × (100% + 1×10%) = 1650 × 1.1 = 1815원
    /// 3. 추가 배율: 1815 × 1.0 = 1815원
    /// </summary>
    [Test]
    public void CalculateReward_MainMatchOneSideWrong_Returns1815()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var sideX = CreateFoodData("I099", "사이드X", FoodType.SIDE);

        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC });

        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema>
        {
            CreateFoodSchema(sideB, 300),
            CreateFoodSchema(sideX, 500) // 틀린 사이드
        };

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(1815, reward, "사이드 1개 틀릴 경우 보상은 1815원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 3: 메인 틀림
    /// 주문: 메인A(1000원), 사이드B(300원)
    /// 제공: 메인X(1500원), 사이드B(300원)
    ///
    /// 계산:
    /// 1. 각 음식: (1500×0.7) + 300 = 1050 + 300 = 1350원
    /// 2. 기본 배율: 1350 × (100% + 1×10%) = 1350 × 1.1 = 1485원
    /// 3. 추가 배율: 1485 × 0.7 = 1039.5원 ≈ 1040원
    /// </summary>
    [Test]
    public void CalculateReward_MainWrong_Returns1040()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var mainX = CreateFoodData("I099", "메인X", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);

        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB });

        var providedMain = CreateFoodSchema(mainX, 1500); // 틀린 메인
        var providedSides = new List<FoodSchema>
        {
            CreateFoodSchema(sideB, 300)
        };

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(1040, reward, "메인 틀릴 경우 보상은 1040원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 4: 사이드 없는 완벽한 주문
    /// 주문: 메인A(2000원)
    /// 제공: 메인A(2000원)
    ///
    /// 계산:
    /// 1. 각 음식: 2000원
    /// 2. 기본 배율: 2000 × (100% + 0×10%) = 2000 × 1.0 = 2000원
    /// 3. 추가 배율: 2000 × 1.3 = 2600원
    /// </summary>
    [Test]
    public void CalculateReward_MainOnlyPerfect_Returns2600()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);

        var order = CreateMenuSchema(mainA, new List<FoodData>());

        var providedMain = CreateFoodSchema(mainA, 2000);
        var providedSides = new List<FoodSchema>();

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(2600, reward, "사이드 없는 완벽한 주문 시 보상은 2600원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 5: 모든 사이드 틀림 (메인만 일치)
    /// 주문: 메인A(1000원), 사이드B(300원), 사이드C(300원)
    /// 제공: 메인A(1000원), 사이드X(400원), 사이드Y(400원)
    ///
    /// 계산:
    /// 1. 각 음식: 1000 + (400×0.7) + (400×0.7) = 1000 + 280 + 280 = 1560원
    /// 2. 기본 배율: 1560 × (100% + 0×10%) = 1560 × 1.0 = 1560원
    /// 3. 추가 배율: 1560 × 1.0 = 1560원
    /// </summary>
    [Test]
    public void CalculateReward_MainMatchAllSidesWrong_Returns1560()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var sideX = CreateFoodData("I098", "사이드X", FoodType.SIDE);
        var sideY = CreateFoodData("I099", "사이드Y", FoodType.SIDE);

        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC });

        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema>
        {
            CreateFoodSchema(sideX, 400),
            CreateFoodSchema(sideY, 400)
        };

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(1560, reward, "메인만 일치하고 모든 사이드가 틀릴 경우 보상은 1560원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 6: 주문보다 많은 사이드 제공 (모두 일치)
    /// 주문: 메인A(1000원), 사이드B(300원)
    /// 제공: 메인A(1000원), 사이드B(300원), 사이드X(200원)
    ///
    /// 계산:
    /// 1. 각 음식: 1000 + 300 + (200×0.7) = 1440원
    /// 2. 기본 배율: 1440 × (100% + 1×10%) = 1440 × 1.1 = 1584원
    /// 3. 추가 배율: 1584 × 1.0 = 1584원 (사이드 개수 불일치로 완벽 아님)
    /// </summary>
    [Test]
    public void CalculateReward_ExtraSide_Returns1584()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideX = CreateFoodData("I099", "사이드X", FoodType.SIDE);

        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB });

        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema>
        {
            CreateFoodSchema(sideB, 300),
            CreateFoodSchema(sideX, 200) // 추가 사이드
        };

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(1584, reward, "추가 사이드 제공 시 보상은 1584원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 7: null 처리 - order가 null
    /// </summary>
    [Test]
    public void CalculateReward_NullOrder_Returns0()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var providedMain = CreateFoodSchema(mainA, 1000);

        // Act
        int reward = MenuValidator.CalculateReward(null, providedMain, null);

        // Assert
        Assert.AreEqual(0, reward, "주문이 null일 경우 보상은 0원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 8: null 처리 - providedMain이 null
    /// </summary>
    [Test]
    public void CalculateReward_NullProvidedMain_Returns0()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var order = CreateMenuSchema(mainA, new List<FoodData>());

        // Act
        int reward = MenuValidator.CalculateReward(order, null, null);

        // Assert
        Assert.AreEqual(0, reward, "제공된 메인이 null일 경우 보상은 0원이어야 합니다.");
    }

    /// <summary>
    /// 테스트 9: 사이드 3개 모두 일치
    /// 주문: 메인A(1000원), 사이드B(200원), 사이드C(200원), 사이드D(200원)
    /// 제공: 메인A(1000원), 사이드B(200원), 사이드C(200원), 사이드D(200원)
    ///
    /// 계산:
    /// 1. 각 음식: 1000 + 200 + 200 + 200 = 1600원
    /// 2. 기본 배율: 1600 × (100% + 3×10%) = 1600 × 1.3 = 2080원
    /// 3. 추가 배율: 2080 × 1.3 = 2704원
    /// </summary>
    [Test]
    public void CalculateReward_ThreeSidesPerfect_Returns2704()
    {
        // Arrange
        var mainA = CreateFoodData("I001", "메인A", FoodType.MAIN);
        var sideB = CreateFoodData("I002", "사이드B", FoodType.SIDE);
        var sideC = CreateFoodData("I003", "사이드C", FoodType.SIDE);
        var sideD = CreateFoodData("I004", "사이드D", FoodType.SIDE);

        var order = CreateMenuSchema(mainA, new List<FoodData> { sideB, sideC, sideD });

        var providedMain = CreateFoodSchema(mainA, 1000);
        var providedSides = new List<FoodSchema>
        {
            CreateFoodSchema(sideB, 200),
            CreateFoodSchema(sideC, 200),
            CreateFoodSchema(sideD, 200)
        };

        // Act
        int reward = MenuValidator.CalculateReward(order, providedMain, providedSides);

        // Assert
        Assert.AreEqual(2704, reward, "사이드 3개 완벽 일치 시 보상은 2704원이어야 합니다.");
    }
}
