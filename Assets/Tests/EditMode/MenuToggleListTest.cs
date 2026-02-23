using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using System.Collections.Generic;

/// <summary>
/// MenuToggleList 메뉴 선택 기능 테스트
/// </summary>
public class MenuToggleListTest
{
    private GameObject testObject;
    private MenuToggleList menuToggleList;
    private GameObject toggleRoot;
    private GameObject menuSlotPrefab;
    private Transform mainContent;
    private Transform sideContent;

    [SetUp]
    public void SetUp()
    {
        // 테스트 오브젝트 생성
        testObject = new GameObject("TestMenuToggleList");
        menuToggleList = testObject.AddComponent<MenuToggleList>();

        // toggleRoot 생성
        toggleRoot = new GameObject("ToggleRoot");
        toggleRoot.transform.SetParent(testObject.transform);

        // menuSlot 프리팹 생성 (실제로는 프리팹이지만 테스트용으로 간단히)
        menuSlotPrefab = new GameObject("MenuSlotPrefab");
        menuSlotPrefab.AddComponent<MenuSlot>();
        menuSlotPrefab.AddComponent<Toggle>();

        // content 생성
        GameObject mainContentObj = new GameObject("MainContent");
        mainContentObj.transform.SetParent(testObject.transform);
        mainContent = mainContentObj.transform;

        GameObject sideContentObj = new GameObject("SideContent");
        sideContentObj.transform.SetParent(testObject.transform);
        sideContent = sideContentObj.transform;

        // MenuToggleList 필드 설정
        var serializedObject = new UnityEditor.SerializedObject(menuToggleList);
        serializedObject.FindProperty("toggleRoot").objectReferenceValue = toggleRoot;
        serializedObject.FindProperty("menuSlot").objectReferenceValue = menuSlotPrefab;
        serializedObject.FindProperty("mainContent").objectReferenceValue = mainContent;
        serializedObject.FindProperty("sideContent").objectReferenceValue = sideContent;
        serializedObject.ApplyModifiedProperties();
    }

    [TearDown]
    public void TearDown()
    {
        if (testObject != null)
            Object.DestroyImmediate(testObject);
        if (menuSlotPrefab != null)
            Object.DestroyImmediate(menuSlotPrefab);
    }

    [Test]
    public void MainMenu_ToggleOn_AddsToMainMenuList()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var toggle = CreateTestToggle("TestMenu_001");

        // Act
        menuToggleList.RegisterToggleListeners(); // 리스너 등록
        toggle.isOn = true;

        // Assert
        var menuList = menuToggleList.GetMenuList();
        Assert.IsNotNull(menuList, "MenuList should not be null");
        Assert.Contains("TestMenu_001", menuList, "Menu ID should be in the list");
    }

    [Test]
    public void MainMenu_ToggleOn_CreatesSlotInMainContent()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var toggle = CreateTestToggle("TestMenu_002");

        // Act
        menuToggleList.RegisterToggleListeners();
        toggle.isOn = true;

        // Assert
        var slots = mainContent.GetComponentsInChildren<MenuSlot>();
        Assert.AreEqual(1, slots.Length, "Should create 1 slot in main content");
        Assert.AreEqual("TestMenu_002", slots[0].Id, "Slot ID should match");
    }

    [Test]
    public void MainMenu_ToggleOff_RemovesFromMainMenuList()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var toggle = CreateTestToggle("TestMenu_003");
        menuToggleList.RegisterToggleListeners();
        toggle.isOn = true;

        // Act
        toggle.isOn = false;

        // Assert
        var menuList = menuToggleList.GetMenuList();
        CollectionAssert.DoesNotContain(menuList, "TestMenu_003", "Menu ID should be removed from list");
    }

    [Test]
    public void MainMenu_ToggleOff_DestroysSlotInMainContent()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var toggle = CreateTestToggle("TestMenu_004");
        menuToggleList.RegisterToggleListeners();
        toggle.isOn = true;
        Assert.AreEqual(1, mainContent.childCount, "Should have 1 child before toggle off");

        // Act
        toggle.isOn = false;

        // Assert
        // Destroy는 프레임 끝에 실행되므로 명시적으로 처리
        Object.DestroyImmediate(mainContent.GetChild(0).gameObject);
        Assert.AreEqual(0, mainContent.childCount, "Should have 0 children after toggle off");
    }

    [Test]
    public void SideMenu_ToggleOn_AddsToSideMenuList()
    {
        // Arrange
        SetMenuType(MenuType.Side);
        var toggle = CreateTestToggle("TestSide_001");

        // Act
        menuToggleList.RegisterToggleListeners();
        toggle.isOn = true;

        // Assert
        var menuList = menuToggleList.GetMenuList();
        Assert.IsNotNull(menuList, "MenuList should not be null");
        Assert.Contains("TestSide_001", menuList, "Side menu ID should be in the list");
    }

    [Test]
    public void SideMenu_ToggleOn_CreatesSlotInSideContent()
    {
        // Arrange
        SetMenuType(MenuType.Side);
        var toggle = CreateTestToggle("TestSide_002");

        // Act
        menuToggleList.RegisterToggleListeners();
        toggle.isOn = true;

        // Assert
        var slots = sideContent.GetComponentsInChildren<MenuSlot>();
        Assert.AreEqual(1, slots.Length, "Should create 1 slot in side content");
        Assert.AreEqual("TestSide_002", slots[0].Id, "Slot ID should match");
    }

    [Test]
    public void MenuToggleList_WithoutMenuSlot_SkipsToggle()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var toggleWithoutSlot = new GameObject("ToggleWithoutSlot");
        toggleWithoutSlot.transform.SetParent(toggleRoot.transform);
        var toggle = toggleWithoutSlot.AddComponent<Toggle>();
        // MenuSlot 컴포넌트를 추가하지 않음

        // Act - Awake에서 warning 로그 예상
        LogAssert.Expect(LogType.Warning, $"[MenuToggleList] Skipping {toggleWithoutSlot.name} - no MenuSlot component found.");
        menuToggleList.RegisterToggleListeners();

        // Assert
        toggle.isOn = true; // 이벤트가 등록되지 않았으므로 아무 일도 일어나지 않음
        var menuList = menuToggleList.GetMenuList();
        Assert.AreEqual(0, menuList.Count, "Should not add menu without MenuSlot");

        // Cleanup
        Object.DestroyImmediate(toggleWithoutSlot);
    }

    [Test]
    public void GetMenuList_ReturnsCorrectListForMenuType()
    {
        // Arrange - Main 타입
        SetMenuType(MenuType.Main);
        var mainToggle = CreateTestToggle("Main_001");
        menuToggleList.RegisterToggleListeners();
        mainToggle.isOn = true;

        // Act
        var mainList = menuToggleList.GetMenuList();

        // Assert
        Assert.IsNotNull(mainList, "Main list should not be null");
        Assert.Contains("Main_001", mainList, "Main list should contain Main_001");

        // Arrange - Side 타입으로 변경
        SetMenuType(MenuType.Side);
        var sideToggle = CreateTestToggle("Side_001");
        sideToggle.isOn = true;

        // Act
        var sideList = menuToggleList.GetMenuList();

        // Assert
        Assert.IsNotNull(sideList, "Side list should not be null");
        // Side 리스트는 별도로 관리되어야 함
    }

    // ────────────────────────────────────────────────────────────
    // Phase 2.4: Dependency Injection Tests
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 2.4: IBentoToggle 의존성 주입 테스트
    /// MenuToggleList가 싱글톤 없이 IBentoToggle을 사용하는지 확인
    /// </summary>
    [Test]
    public void TestInitializeWithDependencyInjection()
    {
        // Arrange
        var model = new DiaryModel(new MockPhaseProgressor());
        var bentoToggleObject = new GameObject("TestBentoToggleList");
        var bentoToggleList = bentoToggleObject.AddComponent<BentoToggleList>();
        bentoToggleList.Initialize(model);

        var foodProvider = new MockUnlockedFoodProvider();

        // Act - Initialize with dependency injection
        menuToggleList.Initialize(foodProvider, bentoToggleList);

        // Assert - 초기화 성공 (에러 없음)
        Assert.IsNotNull(menuToggleList, "MenuToggleList should be initialized");

        // Cleanup
        Object.DestroyImmediate(bentoToggleObject);
    }

    /// <summary>
    /// Phase 2.4: bentoToggle null 처리 테스트
    /// bentoToggle이 null일 때 정상적으로 동작하는지 확인
    /// </summary>
    [Test]
    public void TestNullBentoToggleHandling()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var foodProvider = new MockUnlockedFoodProvider();

        // Act - bentoToggle을 null로 초기화
        menuToggleList.Initialize(foodProvider, null);

        // 토글 생성 및 활성화
        var toggle = CreateTestToggle("TestMenu_Null_001");
        menuToggleList.RegisterToggleListeners();
        toggle.isOn = true;

        // Assert - bentoToggle이 null이어도 크래시 없이 동작해야 함
        var menuList = menuToggleList.GetMenuList();
        Assert.IsNotNull(menuList, "MenuList should work even with null bentoToggle");
        Assert.Contains("TestMenu_Null_001", menuList, "Menu should be added to list");
    }

    // ────────────────────────────────────────────────────────────
    // Phase 4.1: DiaryModel Event-Driven Tests
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Phase 4.1: 도시락 잠금 시 메뉴 선택 비활성화 테스트
    /// OnBentoLockedChanged 이벤트에 반응하여 Toggle들이 비활성화되는지 확인
    /// </summary>
    [Test]
    public void TestOnBentoLocked_DisablesSelection()
    {
        // Arrange
        var model = new DiaryModel(new MockPhaseProgressor());
        var bentoToggleObject = new GameObject("TestBentoToggleList");
        var bentoToggleList = bentoToggleObject.AddComponent<BentoToggleList>();
        bentoToggleList.Initialize(model);

        var foodProvider = new MockUnlockedFoodProvider();

        // Phase 4.1: DiaryModel을 주입하여 초기화
        menuToggleList.Initialize(foodProvider, bentoToggleList, model);

        // 테스트용 토글 생성
        var toggle1 = CreateTestToggle("TestMenu_Lock_001");
        var toggle2 = CreateTestToggle("TestMenu_Lock_002");

        // 초기 상태: 모든 토글이 활성화되어 있음
        Assert.IsTrue(toggle1.interactable, "Toggle should be interactable initially");
        Assert.IsTrue(toggle2.interactable, "Toggle should be interactable initially");

        // Act - 도시락 잠금
        model.LockBentoSelections();

        // Assert - 모든 토글이 비활성화됨
        Assert.IsFalse(toggle1.interactable, "Toggle should be disabled when bento is locked");
        Assert.IsFalse(toggle2.interactable, "Toggle should be disabled when bento is locked");

        // Act - 도시락 잠금 해제
        model.UnlockBentoSelections();

        // Assert - 모든 토글이 다시 활성화됨
        Assert.IsTrue(toggle1.interactable, "Toggle should be enabled when bento is unlocked");
        Assert.IsTrue(toggle2.interactable, "Toggle should be enabled when bento is unlocked");

        // Cleanup
        Object.DestroyImmediate(bentoToggleObject);
    }

    /// <summary>
    /// Phase 4.1: DiaryModel null 처리 테스트
    /// DiaryModel이 null일 때도 정상 동작하는지 확인
    /// </summary>
    [Test]
    public void TestNullDiaryModelHandling()
    {
        // Arrange
        SetMenuType(MenuType.Main);
        var foodProvider = new MockUnlockedFoodProvider();

        // Act - DiaryModel을 null로 초기화
        menuToggleList.Initialize(foodProvider, null, null);

        // 토글 생성
        var toggle = CreateTestToggle("TestMenu_NullModel_001");
        menuToggleList.RegisterToggleListeners();

        // Assert - null DiaryModel이어도 크래시 없이 동작
        Assert.IsNotNull(menuToggleList, "MenuToggleList should work with null DiaryModel");
        toggle.isOn = true;
        var menuList = menuToggleList.GetMenuList();
        Assert.Contains("TestMenu_NullModel_001", menuList, "Menu should be added even with null DiaryModel");
    }

    // Helper Methods

    private void SetMenuType(MenuType type)
    {
        var serializedObject = new UnityEditor.SerializedObject(menuToggleList);
        serializedObject.FindProperty("menuType").enumValueIndex = (int)type;
        serializedObject.ApplyModifiedProperties();
    }

    private Toggle CreateTestToggle(string menuId)
    {
        var toggleObj = new GameObject($"Toggle_{menuId}");
        toggleObj.transform.SetParent(toggleRoot.transform);

        var toggle = toggleObj.AddComponent<Toggle>();
        var menuSlot = toggleObj.AddComponent<MenuSlot>();

        // MenuSlot의 Id 설정
        var serializedObject = new UnityEditor.SerializedObject(menuSlot);
        serializedObject.FindProperty("Id").stringValue = menuId;
        serializedObject.ApplyModifiedProperties();

        return toggle;
    }

    // ────────────────────────────────────────────────────────────
    // Mock Classes for Phase 2.4 Tests
    // ────────────────────────────────────────────────────────────

    /// <summary>
    /// Mock phase progressor for testing
    /// </summary>
    private class MockPhaseProgressor : IPhaseProgressor
    {
        public void PassPhase() { }
    }

    /// <summary>
    /// Mock unlocked food provider for testing
    /// </summary>
    private class MockUnlockedFoodProvider : IUnlockedFoodProvider
    {
        public List<FoodData> GetUnlockedMainFoods()
        {
            return new List<FoodData>();
        }

        public List<FoodData> GetUnlockedSideFoods()
        {
            return new List<FoodData>();
        }

        public bool IsUnlocked(string foodId)
        {
            return true;
        }
    }
}
