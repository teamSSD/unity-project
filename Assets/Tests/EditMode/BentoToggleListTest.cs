using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Phase 3.3: BentoToggleList 이벤트 기반 전환 테스트
/// Pure UI Adapter 패턴 및 이벤트 구독 검증
/// </summary>
public class BentoToggleListTest
{
    private GameObject testObject;
    private BentoToggleList bentoToggleList;
    private DiaryModel model;
    private MockPhaseProgressor phaseProgressor;
    private GameObject toggleRoot;

    // 테스트용 FoodData
    private FoodData testMainFood;
    private FoodData testSideFood1;
    private FoodData testSideFood2;

    [SetUp]
    public void SetUp()
    {
        // 테스트 오브젝트 생성
        testObject = new GameObject("TestBentoToggleList");
        bentoToggleList = testObject.AddComponent<BentoToggleList>();

        // toggleRoot 생성
        toggleRoot = new GameObject("ToggleRoot");
        toggleRoot.transform.SetParent(testObject.transform);

        // SerializedObject로 toggleRoot 설정
        var serializedObject = new UnityEditor.SerializedObject(bentoToggleList);
        serializedObject.FindProperty("toggleRoot").objectReferenceValue = toggleRoot;
        serializedObject.ApplyModifiedProperties();

        // DiaryModel 생성
        phaseProgressor = new MockPhaseProgressor();
        model = new DiaryModel(phaseProgressor);

        // 테스트용 FoodData 생성
        testMainFood = ScriptableObject.CreateInstance<FoodData>();
        testMainFood.id = "TestMain_001";
        testMainFood.ingredientName = "Test Main";
        testMainFood.type = FoodType.MAIN;

        testSideFood1 = ScriptableObject.CreateInstance<FoodData>();
        testSideFood1.id = "TestSide_001";
        testSideFood1.ingredientName = "Test Side 1";
        testSideFood1.type = FoodType.SIDE;

        testSideFood2 = ScriptableObject.CreateInstance<FoodData>();
        testSideFood2.id = "TestSide_002";
        testSideFood2.ingredientName = "Test Side 2";
        testSideFood2.type = FoodType.SIDE;
    }

    [TearDown]
    public void TearDown()
    {
        if (testObject != null)
            Object.DestroyImmediate(testObject);
        if (testMainFood != null)
            Object.DestroyImmediate(testMainFood);
        if (testSideFood1 != null)
            Object.DestroyImmediate(testSideFood1);
        if (testSideFood2 != null)
            Object.DestroyImmediate(testSideFood2);
    }

    // ────────────────────────────────────────────────────────────
    // Phase 3.3: 기본 이벤트 기반 테스트
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialize 시 DiaryModel 이벤트에 구독하는지 검증
    /// </summary>
    [Test]
    public void TestInitialize_SubscribesToModelEvents()
    {
        // Arrange
        int addedEventCount = 0;
        int removedEventCount = 0;
        int lockedEventCount = 0;

        model.OnBentoFoodAdded += (index, food) => addedEventCount++;
        model.OnBentoFoodRemoved += (index, food) => removedEventCount++;
        model.OnBentoLockedChanged += (isLocked) => lockedEventCount++;

        // Act
        bentoToggleList.Initialize(model);

        // Model에서 이벤트 발행
        model.AddBentoFood(0, testMainFood);
        model.RemoveBentoFood(0, testMainFood);
        model.LockBentoSelections();

        // Assert
        Assert.AreEqual(1, addedEventCount, "OnBentoFoodAdded event should be fired once");
        Assert.AreEqual(1, removedEventCount, "OnBentoFoodRemoved event should be fired once");
        Assert.AreEqual(1, lockedEventCount, "OnBentoLockedChanged event should be fired once");
    }

    /// <summary>
    /// AddFood 호출 시 DiaryModel에 위임하고 이벤트가 발행되는지 검증
    /// </summary>
    [Test]
    public void TestAddFood_DelegatesToModelAndTriggersEvent()
    {
        // Arrange
        bentoToggleList.Initialize(model);

        bool eventFired = false;
        int firedBentoIndex = -1;
        FoodData firedFood = null;

        model.OnBentoFoodAdded += (index, food) =>
        {
            eventFired = true;
            firedBentoIndex = index;
            firedFood = food;
        };

        // Act
        bentoToggleList.AddFood(testMainFood);

        // Assert
        Assert.IsTrue(eventFired, "OnBentoFoodAdded event should be fired");
        Assert.AreEqual(0, firedBentoIndex, "Event should fire with bento index 0");
        Assert.AreEqual(testMainFood, firedFood, "Event should fire with correct food");

        // Model 상태 확인
        var bento = model.GetBentoForDisplay(0);
        Assert.AreEqual(testMainFood, bento.MainMenu, "Food should be added to Model");
    }

    /// <summary>
    /// RemoveFood 호출 시 DiaryModel에 위임하고 이벤트가 발행되는지 검증
    /// </summary>
    [Test]
    public void TestRemoveFood_DelegatesToModelAndTriggersEvent()
    {
        // Arrange
        bentoToggleList.Initialize(model);
        model.AddBentoFood(0, testMainFood); // 먼저 추가

        bool eventFired = false;
        int firedBentoIndex = -1;
        FoodData firedFood = null;

        model.OnBentoFoodRemoved += (index, food) =>
        {
            eventFired = true;
            firedBentoIndex = index;
            firedFood = food;
        };

        // Act
        bentoToggleList.RemoveFood(testMainFood);

        // Assert
        Assert.IsTrue(eventFired, "OnBentoFoodRemoved event should be fired");
        Assert.AreEqual(0, firedBentoIndex, "Event should fire with bento index 0");
        Assert.AreEqual(testMainFood, firedFood, "Event should fire with correct food");

        // Model 상태 확인
        var bento = model.GetBentoForDisplay(0);
        Assert.IsNull(bento.MainMenu, "Food should be removed from Model");
    }

    /// <summary>
    /// OnLockStateChanged 이벤트 발생 시 SetInteractivity 호출되는지 검증
    /// </summary>
    [Test]
    public void TestOnLockStateChanged_UpdatesInteractable()
    {
        // Arrange
        bentoToggleList.Initialize(model);

        // CanvasGroup 추가 (SetInteractivity가 사용)
        var canvasGroup = testObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        // Act - 잠금
        model.LockBentoSelections();

        // Assert
        Assert.IsFalse(canvasGroup.interactable, "CanvasGroup should be non-interactable when locked");
        Assert.IsFalse(canvasGroup.blocksRaycasts, "CanvasGroup should not block raycasts when locked");

        // Act - 잠금 해제
        model.UnlockBentoSelections();

        // Assert
        Assert.IsTrue(canvasGroup.interactable, "CanvasGroup should be interactable when unlocked");
        Assert.IsTrue(canvasGroup.blocksRaycasts, "CanvasGroup should block raycasts when unlocked");
    }

    /// <summary>
    /// HasAnySelection이 Model 데이터를 올바르게 반환하는지 검증
    /// </summary>
    [Test]
    public void TestHasAnySelection_UsesModelData()
    {
        // Arrange
        bentoToggleList.Initialize(model);

        // Act - 초기 상태 (선택 없음)
        bool hasSelection = bentoToggleList.HasAnySelection();

        // Assert
        Assert.IsFalse(hasSelection, "Should have no selection initially");

        // Act - 메뉴 추가 후
        model.AddBentoFood(0, testMainFood);
        hasSelection = bentoToggleList.HasAnySelection();

        // Assert
        Assert.IsTrue(hasSelection, "Should have selection after adding food");

        // Act - 메뉴 제거 후
        model.RemoveBentoFood(0, testMainFood);
        hasSelection = bentoToggleList.HasAnySelection();

        // Assert
        Assert.IsFalse(hasSelection, "Should have no selection after removing all food");
    }

    /// <summary>
    /// ToString이 Model 데이터를 올바르게 표시하는지 검증
    /// </summary>
    [Test]
    public void TestToString_DisplaysModelData()
    {
        // Arrange
        bentoToggleList.Initialize(model);
        model.AddBentoFood(0, testMainFood);
        model.AddBentoFood(0, testSideFood1);

        // Act
        string summary = bentoToggleList.ToString();

        // Assert
        Assert.IsTrue(summary.Contains(testMainFood.ingredientName), "Summary should contain main menu name");
        Assert.IsTrue(summary.Contains(testSideFood1.ingredientName), "Summary should contain side menu name");
        Assert.IsTrue(summary.Contains("Bento 1"), "Summary should contain bento name");
    }

    /// <summary>
    /// OnDestroy 시 이벤트 구독 해제되는지 검증
    /// </summary>
    [Test]
    public void TestOnDestroy_UnsubscribesFromEvents()
    {
        // Arrange
        bentoToggleList.Initialize(model);

        int eventCount = 0;
        model.OnBentoFoodAdded += (index, food) => eventCount++;

        // Act - 이벤트 발행 (구독 중)
        model.AddBentoFood(0, testMainFood);
        int countBeforeDestroy = eventCount;

        // Destroy
        Object.DestroyImmediate(testObject);
        testObject = null; // TearDown에서 다시 파괴하지 않도록

        // 이벤트 다시 발행 (구독 해제됨)
        model.AddBentoFood(1, testSideFood1);

        // Assert
        Assert.AreEqual(1, countBeforeDestroy, "Event should fire before destroy");
        Assert.AreEqual(1, eventCount, "Event should not fire after destroy (unsubscribed)");
    }

    /// <summary>
    /// 잠금 상태에서 AddFood 호출 시 Model이 거부하는지 검증
    /// </summary>
    [Test]
    public void TestAddFood_WhenLocked_ModelRejects()
    {
        // Arrange
        bentoToggleList.Initialize(model);
        model.LockBentoSelections();

        bool eventFired = false;
        model.OnBentoFoodAdded += (index, food) => eventFired = true;

        // Act
        bentoToggleList.AddFood(testMainFood);

        // Assert
        Assert.IsFalse(eventFired, "OnBentoFoodAdded should not fire when locked");
        var bento = model.GetBentoForDisplay(0);
        Assert.IsNull(bento.MainMenu, "Food should not be added when locked");
    }

    /// <summary>
    /// 잠금 상태에서 RemoveFood 호출 시 Model이 거부하는지 검증
    /// </summary>
    [Test]
    public void TestRemoveFood_WhenLocked_ModelRejects()
    {
        // Arrange
        bentoToggleList.Initialize(model);
        model.AddBentoFood(0, testMainFood); // 먼저 추가
        model.LockBentoSelections(); // 잠금

        bool eventFired = false;
        model.OnBentoFoodRemoved += (index, food) => eventFired = true;

        // Act
        bentoToggleList.RemoveFood(testMainFood);

        // Assert
        Assert.IsFalse(eventFired, "OnBentoFoodRemoved should not fire when locked");
        var bento = model.GetBentoForDisplay(0);
        Assert.AreEqual(testMainFood, bento.MainMenu, "Food should not be removed when locked");
    }

    // ────────────────────────────────────────────────────────────
    // Mock Classes
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Mock phase progressor for testing
    /// </summary>
    private class MockPhaseProgressor : IPhaseProgressor
    {
        public void PassPhase() { }
    }
}
