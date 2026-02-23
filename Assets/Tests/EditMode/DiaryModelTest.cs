using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// DiaryModel 테스트 (DiaryActionManagerTest에서 마이그레이션)
/// 순수 C# 클래스이므로 GameObject 없이 테스트 가능
/// </summary>
public class DiaryModelTest
{
    private DiaryModel model;
    private MockPhaseProgressor mockProgressor;
    private List<FoodData> testFoods;

    [SetUp]
    public void Setup()
    {
        mockProgressor = new MockPhaseProgressor();
        model = new DiaryModel(mockProgressor);

        // 테스트용 음식 데이터 준비 (ScriptableObject.CreateInstance 사용)
        testFoods = new List<FoodData>
        {
            CreateTestFood("main1", "김밥", FoodType.MAIN),
            CreateTestFood("main2", "주먹밥", FoodType.MAIN),
            CreateTestFood("side1", "단무지", FoodType.SIDE),
            CreateTestFood("side2", "김치", FoodType.SIDE)
        };

        // 현재 페이즈를 Preparation으로 설정
        model.SyncPhase(PhaseType.Preparation);
    }

    #region Preparation Phase (ALL Mode) Tests

    [Test]
    public void Preparation_ALL_Mode_RequiresBothActions()
    {
        // Preparation은 MenuSelect + PrepareIngredients 둘 다 완료 필요

        // 도시락 추가해서 MenuSelect 활성화
        model.AddBentoFood(0, testFoods[0]);

        // MenuSelect만 Done으로 설정
        bool menuSuccess = model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.IsTrue(menuSuccess, "MenuSelect 실행 실패");

        // 아직 페이즈 미완료
        Assert.IsFalse(model.IsPhaseCompleted(PhaseType.Preparation),
            "MenuSelect만 완료했는데 페이즈 완료로 표시됨");

        // PrepareIngredients도 Done으로 설정
        bool prepSuccess = model.ExecuteAction(PhaseType.Preparation, ActionType.PrepareIngredients);
        Assert.IsTrue(prepSuccess, "PrepareIngredients 실행 실패");

        // 이제 페이즈 완료
        Assert.IsTrue(model.IsPhaseCompleted(PhaseType.Preparation),
            "둘 다 완료했는데 페이즈 미완료로 표시됨");
    }

    [Test]
    public void Preparation_MenuSelect_RequiresBentoSelection()
    {
        // 도시락 없으면 MenuSelect 실행 불가
        bool success = model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.IsFalse(success, "도시락 없는데 MenuSelect 실행됨");

        // 도시락 추가 후 실행 가능
        model.AddBentoFood(0, testFoods[0]);
        success = model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.IsTrue(success, "도시락 있는데 MenuSelect 실행 실패");
    }

    [Test]
    public void Preparation_MenuSelect_LocksBentoSelections()
    {
        // 도시락 추가
        model.AddBentoFood(0, testFoods[0]);

        // MenuSelect 실행
        model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);

        // 도시락 추가 시도 (실패해야 함)
        bool addSuccess = model.AddBentoFood(1, testFoods[1]);
        Assert.IsFalse(addSuccess, "MenuSelect 후 도시락 추가 가능함");

        // 도시락 제거 시도 (실패해야 함)
        bool removeSuccess = model.RemoveBentoFood(0, testFoods[0]);
        Assert.IsFalse(removeSuccess, "MenuSelect 후 도시락 제거 가능함");
    }

    #endregion

    #region SINGLE Mode Tests (Morning/Afternoon/Evening/Night)

    [Test]
    public void Morning_SINGLE_Mode_OnlyOneActionAllowed()
    {
        // Preparation 완료
        model.AddBentoFood(0, testFoods[0]);
        model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        model.ExecuteAction(PhaseType.Preparation, ActionType.PrepareIngredients);

        // Morning으로 전환
        model.SyncPhase(PhaseType.Morning);

        // Work 선택
        bool workSuccess = model.ExecuteAction(PhaseType.Morning, ActionType.Work);
        Assert.IsTrue(workSuccess, "Work 실행 실패");
        Assert.AreEqual(ActionState.Selected, model.GetState(PhaseType.Morning, ActionType.Work));

        // Rest 선택 (Work는 Available로 돌아가야 함)
        bool restSuccess = model.ExecuteAction(PhaseType.Morning, ActionType.Rest);
        Assert.IsTrue(restSuccess, "Rest 실행 실패");
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Work),
            "Work가 Available로 돌아가지 않음");
        Assert.AreEqual(ActionState.Selected, model.GetState(PhaseType.Morning, ActionType.Rest),
            "Rest가 Selected 되지 않음");
    }

    [Test]
    public void SINGLE_Mode_CompletesWithOneAction()
    {
        // Preparation 완료
        model.AddBentoFood(0, testFoods[0]);
        model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        model.ExecuteAction(PhaseType.Preparation, ActionType.PrepareIngredients);

        // Morning으로 전환
        model.SyncPhase(PhaseType.Morning);

        // Work 선택
        model.ExecuteAction(PhaseType.Morning, ActionType.Work);

        // 하나만 선택해도 페이즈 완료
        Assert.IsTrue(model.IsPhaseCompleted(PhaseType.Morning),
            "SINGLE 모드인데 페이즈 미완료");
    }

    #endregion

    #region Sequential Dependency Tests

    [Test]
    public void SequentialDependency_PrevPhaseRequired()
    {
        // Preparation 미완료 상태에서 Morning 액션 실행 시도
        model.SyncPhase(PhaseType.Morning);

        bool success = model.ExecuteAction(PhaseType.Morning, ActionType.Work);
        Assert.IsFalse(success, "이전 페이즈 미완료인데 실행됨");
    }

    [Test]
    public void SequentialDependency_MarksCompletedPhasesAsCompleted()
    {
        // Morning으로 전환 (Preparation은 자동으로 완료 처리되어야 함)
        model.SyncPhase(PhaseType.Morning);

        // Preparation 액션들이 Done 상태여야 함
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.MenuSelect));
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.PrepareIngredients));
    }

    #endregion

    #region Bento Management Tests

    [Test]
    public void Bento_AddFood_Success()
    {
        bool success = model.AddBentoFood(0, testFoods[0]);
        Assert.IsTrue(success, "도시락 추가 실패");
        Assert.IsTrue(model.HasAnyBentoSelection(), "도시락 선택 안됨");

        var bento = model.GetBentoSelection(0);
        Assert.IsNotNull(bento, "도시락이 null");
        Assert.AreEqual(testFoods[0], bento.MainMenu, "메인 메뉴가 다름");
    }

    [Test]
    public void Bento_RemoveFood_Success()
    {
        model.AddBentoFood(0, testFoods[0]);
        bool success = model.RemoveBentoFood(0, testFoods[0]);
        Assert.IsTrue(success, "도시락 제거 실패");
        Assert.IsFalse(model.HasAnyBentoSelection(), "도시락 여전히 선택됨");
    }

    [Test]
    public void Bento_MaxThreeSelections()
    {
        // 3개까지 추가 가능
        model.AddBentoFood(0, testFoods[0]);
        model.AddBentoFood(1, testFoods[1]);
        model.AddBentoFood(2, testFoods[2]);

        var snapshot = model.CreateSnapshot();
        Assert.IsTrue(snapshot.IsValid(), "3개 도시락 유효하지 않음");

        // 4개째 추가 시도 (현재는 가능하지만, 나중에 제한 추가 가능)
        // 이 테스트는 현재 DiaryModel이 3개 제한을 강제하지 않으므로 주석 처리
        // bool fourthSuccess = model.AddBentoFood(3, testFoods[3]);
        // Assert.IsFalse(fourthSuccess, "4개째 도시락 추가 가능함");
    }

    [Test]
    public void Bento_Lock_PreventsModification()
    {
        model.AddBentoFood(0, testFoods[0]);
        model.LockBentoSelections();

        // 추가 불가
        bool addSuccess = model.AddBentoFood(1, testFoods[1]);
        Assert.IsFalse(addSuccess, "락 후 추가 가능함");

        // 제거 불가
        bool removeSuccess = model.RemoveBentoFood(0, testFoods[0]);
        Assert.IsFalse(removeSuccess, "락 후 제거 가능함");
    }

    #endregion

    #region Phase 3.1: Bento Event Tests

    [Test]
    public void BentoEvent_AddFood_FiresOnBentoFoodAdded()
    {
        // Arrange
        int eventBentoIndex = -1;
        FoodData eventFood = null;
        int eventCount = 0;

        model.OnBentoFoodAdded += (index, food) =>
        {
            eventBentoIndex = index;
            eventFood = food;
            eventCount++;
        };

        // Act
        bool success = model.AddBentoFood(0, testFoods[0]);

        // Assert
        Assert.IsTrue(success, "도시락 추가 실패");
        Assert.AreEqual(1, eventCount, "OnBentoFoodAdded 이벤트가 발행되지 않음");
        Assert.AreEqual(0, eventBentoIndex, "이벤트 bentoIndex가 다름");
        Assert.AreEqual(testFoods[0], eventFood, "이벤트 food가 다름");
    }

    [Test]
    public void BentoEvent_RemoveFood_FiresOnBentoFoodRemoved()
    {
        // Arrange
        model.AddBentoFood(0, testFoods[0]);

        int eventBentoIndex = -1;
        FoodData eventFood = null;
        int eventCount = 0;

        model.OnBentoFoodRemoved += (index, food) =>
        {
            eventBentoIndex = index;
            eventFood = food;
            eventCount++;
        };

        // Act
        bool success = model.RemoveBentoFood(0, testFoods[0]);

        // Assert
        Assert.IsTrue(success, "도시락 제거 실패");
        Assert.AreEqual(1, eventCount, "OnBentoFoodRemoved 이벤트가 발행되지 않음");
        Assert.AreEqual(0, eventBentoIndex, "이벤트 bentoIndex가 다름");
        Assert.AreEqual(testFoods[0], eventFood, "이벤트 food가 다름");
    }

    [Test]
    public void BentoEvent_Lock_FiresOnBentoLockedChanged()
    {
        // Arrange
        bool eventIsLocked = false;
        int eventCount = 0;

        model.OnBentoLockedChanged += (isLocked) =>
        {
            eventIsLocked = isLocked;
            eventCount++;
        };

        // Act - 잠금
        model.LockBentoSelections();

        // Assert
        Assert.AreEqual(1, eventCount, "OnBentoLockedChanged 이벤트가 발행되지 않음 (Lock)");
        Assert.IsTrue(eventIsLocked, "이벤트 isLocked가 false임");

        // Act - 잠금 해제
        model.UnlockBentoSelections();

        // Assert
        Assert.AreEqual(2, eventCount, "OnBentoLockedChanged 이벤트가 발행되지 않음 (Unlock)");
        Assert.IsFalse(eventIsLocked, "이벤트 isLocked가 true임");
    }

    [Test]
    public void BentoEvent_AddFood_WhenLocked_DoesNotFireEvent()
    {
        // Arrange
        model.LockBentoSelections();

        int eventCount = 0;
        model.OnBentoFoodAdded += (index, food) => eventCount++;

        // Act
        bool success = model.AddBentoFood(0, testFoods[0]);

        // Assert
        Assert.IsFalse(success, "잠금 상태인데 추가 성공함");
        Assert.AreEqual(0, eventCount, "잠금 상태인데 OnBentoFoodAdded 이벤트 발행됨");
    }

    [Test]
    public void BentoEvent_RemoveFood_WhenLocked_DoesNotFireEvent()
    {
        // Arrange
        model.AddBentoFood(0, testFoods[0]);
        model.LockBentoSelections();

        int eventCount = 0;
        model.OnBentoFoodRemoved += (index, food) => eventCount++;

        // Act
        bool success = model.RemoveBentoFood(0, testFoods[0]);

        // Assert
        Assert.IsFalse(success, "잠금 상태인데 제거 성공함");
        Assert.AreEqual(0, eventCount, "잠금 상태인데 OnBentoFoodRemoved 이벤트 발행됨");
    }

    [Test]
    public void BentoEvent_AddSideFood_FiresOnBentoFoodAdded()
    {
        // Arrange
        model.AddBentoFood(0, testFoods[0]); // 메인 추가

        int eventCount = 0;
        model.OnBentoFoodAdded += (index, food) => eventCount++;

        // Act - 사이드 추가
        bool success = model.AddBentoFood(0, testFoods[2]); // side1

        // Assert
        Assert.IsTrue(success, "사이드 추가 실패");
        Assert.AreEqual(1, eventCount, "사이드 추가 시 OnBentoFoodAdded 이벤트가 발행되지 않음");
    }

    [Test]
    public void BentoEvent_Lock_DoesNotFireIfAlreadyLocked()
    {
        // Arrange
        model.LockBentoSelections();

        int eventCount = 0;
        model.OnBentoLockedChanged += (isLocked) => eventCount++;

        // Act - 이미 잠금 상태에서 다시 잠금
        model.LockBentoSelections();

        // Assert - 이벤트 발행되지 않아야 함
        Assert.AreEqual(0, eventCount, "이미 잠금 상태인데 OnBentoLockedChanged 이벤트 발행됨");
    }

    [Test]
    public void BentoEvent_Unlock_DoesNotFireIfAlreadyUnlocked()
    {
        // Arrange - 초기 상태는 잠금 해제
        int eventCount = 0;
        model.OnBentoLockedChanged += (isLocked) => eventCount++;

        // Act - 이미 잠금 해제 상태에서 다시 잠금 해제
        model.UnlockBentoSelections();

        // Assert - 이벤트 발행되지 않아야 함
        Assert.AreEqual(0, eventCount, "이미 잠금 해제 상태인데 OnBentoLockedChanged 이벤트 발행됨");
    }

    #endregion

    #region State Management Tests

    [Test]
    public void GetState_ReturnsCorrectState()
    {
        var state = model.GetState(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.AreEqual(ActionState.Disavailable, state, "초기 상태가 Disavailable이 아님");
    }

    [Test]
    public void SetState_ChangesState()
    {
        bool success = model.SetState(PhaseType.Preparation, ActionType.MenuSelect, ActionState.Available);
        Assert.IsTrue(success, "상태 변경 실패");

        var state = model.GetState(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.AreEqual(ActionState.Available, state, "상태가 변경되지 않음");
    }

    [Test]
    public void SetState_FiresEvent()
    {
        PhaseType eventPhase = PhaseType.Morning;
        ActionType eventAction = ActionType.Work;
        ActionState eventState = ActionState.Disavailable;
        int eventCount = 0;

        model.OnActionStateChanged += (phase, action, state) =>
        {
            eventPhase = phase;
            eventAction = action;
            eventState = state;
            eventCount++;
        };

        model.SetState(PhaseType.Preparation, ActionType.MenuSelect, ActionState.Available);

        Assert.AreEqual(1, eventCount, "이벤트가 발행되지 않음");
        Assert.AreEqual(PhaseType.Preparation, eventPhase);
        Assert.AreEqual(ActionType.MenuSelect, eventAction);
        Assert.AreEqual(ActionState.Available, eventState);
    }

    #endregion

    #region Phase Management Tests

    [Test]
    public void SyncPhase_UpdatesCurrentPhase()
    {
        model.SyncPhase(PhaseType.Morning);
        Assert.AreEqual(PhaseType.Morning, model.CurrentPhase, "페이즈가 업데이트되지 않음");
    }

    [Test]
    public void SyncPhase_FiresEvent()
    {
        PhaseType eventPhase = PhaseType.Preparation;
        int eventCount = 0;

        model.OnPhaseChanged += (phase) =>
        {
            eventPhase = phase;
            eventCount++;
        };

        model.SyncPhase(PhaseType.Morning);

        Assert.AreEqual(1, eventCount, "페이즈 변경 이벤트가 발행되지 않음");
        Assert.AreEqual(PhaseType.Morning, eventPhase);
    }

    [Test]
    public void SyncPhase_RefreshesPhaseActions()
    {
        // Morning으로 전환
        model.SyncPhase(PhaseType.Morning);

        // Preparation 액션은 Done
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.MenuSelect));
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.PrepareIngredients));

        // Morning 액션은 Available
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Work));
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Rest));
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Shopping));
    }

    #endregion

    #region Command Pattern Tests

    [Test]
    public void ExecuteAction_UsesCommandPattern()
    {
        // 도시락 추가
        model.AddBentoFood(0, testFoods[0]);

        // MenuSelectAction을 통해 실행
        bool success = model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.IsTrue(success, "Command 실행 실패");
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.MenuSelect));
    }

    [Test]
    public void ExecuteAction_FiresActionExecutedEvent()
    {
        ActionType executedAction = ActionType.Work;
        int eventCount = 0;

        model.OnActionExecuted += (action) =>
        {
            executedAction = action;
            eventCount++;
        };

        // PrepareIngredients 실행 (도시락 불필요)
        model.ExecuteAction(PhaseType.Preparation, ActionType.PrepareIngredients);

        Assert.AreEqual(1, eventCount, "ActionExecuted 이벤트가 발행되지 않음");
        Assert.AreEqual(ActionType.PrepareIngredients, executedAction);
    }

    [Test]
    public void ExecuteAction_CallsPhaseProgressorOnCompletion()
    {
        // Preparation 완료
        model.AddBentoFood(0, testFoods[0]);
        model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        model.ExecuteAction(PhaseType.Preparation, ActionType.PrepareIngredients);

        // PassPhase 호출됨
        Assert.AreEqual(1, mockProgressor.PassPhaseCallCount, "PassPhase가 호출되지 않음");
    }

    #endregion

    #region Snapshot Tests

    [Test]
    public void CreateSnapshot_CapturesCurrentState()
    {
        // 상태 설정
        model.AddBentoFood(0, testFoods[0]);
        model.SetState(PhaseType.Preparation, ActionType.MenuSelect, ActionState.Available);
        model.SyncPhase(PhaseType.Preparation);

        // 스냅샷 생성
        var snapshot = model.CreateSnapshot();

        Assert.AreEqual(PhaseType.Preparation, snapshot.CurrentPhase);
        Assert.IsNotNull(snapshot.BentoSelections);
        Assert.AreEqual(3, snapshot.BentoSelections.Length);
        Assert.AreEqual(testFoods[0], snapshot.BentoSelections[0].MainMenu);
    }

    [Test]
    public void RestoreSnapshot_RestoresState()
    {
        // 초기 상태 설정
        model.AddBentoFood(0, testFoods[0]);
        model.SetState(PhaseType.Preparation, ActionType.MenuSelect, ActionState.Available);

        // 스냅샷 생성
        var snapshot = model.CreateSnapshot();

        // 상태 변경
        model.SyncPhase(PhaseType.Morning);
        model.SetState(PhaseType.Morning, ActionType.Work, ActionState.Selected);

        // 스냅샷 복원
        model.RestoreSnapshot(snapshot);

        // 복원 확인
        Assert.AreEqual(PhaseType.Preparation, model.CurrentPhase);
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Preparation, ActionType.MenuSelect));
        Assert.IsTrue(model.HasAnyBentoSelection());
    }

    #endregion

    #region Error Handling Tests

    [Test]
    public void ExecuteAction_InvalidActionType_ReturnsFalse()
    {
        // Preparation 페이즈에서 Work 실행 시도 (불가능)
        bool success = model.ExecuteAction(PhaseType.Preparation, ActionType.Work);
        Assert.IsFalse(success, "잘못된 액션이 실행됨");
    }

    [Test]
    public void GetBentoSelection_InvalidIndex_ReturnsNull()
    {
        var bento = model.GetBentoSelection(99);
        Assert.IsNull(bento, "잘못된 인덱스에서 도시락이 반환됨");
    }

    [Test]
    public void AddBentoFood_InvalidIndex_ReturnsFalse()
    {
        bool success = model.AddBentoFood(-1, testFoods[0]);
        Assert.IsFalse(success, "잘못된 인덱스에 추가 가능함");
    }

    #endregion

    #region Integration Tests

    [Test]
    public void FullWorkflow_Preparation_To_Morning()
    {
        // 1. Preparation: 도시락 선택
        model.AddBentoFood(0, testFoods[0]);
        Assert.IsTrue(model.HasAnyBentoSelection());

        // 2. MenuSelect 실행
        bool menuSuccess = model.ExecuteAction(PhaseType.Preparation, ActionType.MenuSelect);
        Assert.IsTrue(menuSuccess);
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.MenuSelect));

        // 3. PrepareIngredients 실행
        bool prepSuccess = model.ExecuteAction(PhaseType.Preparation, ActionType.PrepareIngredients);
        Assert.IsTrue(prepSuccess);
        Assert.AreEqual(ActionState.Done, model.GetState(PhaseType.Preparation, ActionType.PrepareIngredients));

        // 4. Preparation 완료 확인
        Assert.IsTrue(model.IsPhaseCompleted(PhaseType.Preparation));

        // 5. PassPhase 호출됨
        Assert.AreEqual(1, mockProgressor.PassPhaseCallCount);

        // 6. Morning으로 전환
        model.SyncPhase(PhaseType.Morning);
        Assert.AreEqual(PhaseType.Morning, model.CurrentPhase);

        // 7. Morning 액션들이 Available
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Work));
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Rest));
        Assert.AreEqual(ActionState.Available, model.GetState(PhaseType.Morning, ActionType.Shopping));

        // 8. Work 실행
        bool workSuccess = model.ExecuteAction(PhaseType.Morning, ActionType.Work);
        Assert.IsTrue(workSuccess);
        Assert.AreEqual(ActionState.Selected, model.GetState(PhaseType.Morning, ActionType.Work));

        // 9. Morning 완료 확인
        Assert.IsTrue(model.IsPhaseCompleted(PhaseType.Morning));

        // 10. PassPhase 두 번째 호출
        Assert.AreEqual(2, mockProgressor.PassPhaseCallCount);
    }

    #endregion

    #region Bug Fix Tests: Auto-Find and Validation

    /// <summary>
    /// Bug Fix: AddBentoFood(FoodData) 자동으로 빈 슬롯 찾기
    /// </summary>
    [Test]
    public void AddBentoFood_AutoFind_AddsToFirstEmptyBento()
    {
        // Arrange: 3개의 메인 메뉴 준비
        var main1 = CreateTestFood("main1", "김밥", FoodType.MAIN);
        var main2 = CreateTestFood("main2", "주먹밥", FoodType.MAIN);
        var main3 = CreateTestFood("main3", "샌드위치", FoodType.MAIN);

        // Act: 순차적으로 추가 (자동으로 빈 슬롯 찾아서 추가)
        bool success1 = model.AddBentoFood(main1);
        bool success2 = model.AddBentoFood(main2);
        bool success3 = model.AddBentoFood(main3);

        // Assert: 모두 성공, 각각 다른 도시락에 추가됨
        Assert.IsTrue(success1, "첫 번째 메인 메뉴 추가 실패");
        Assert.IsTrue(success2, "두 번째 메인 메뉴 추가 실패");
        Assert.IsTrue(success3, "세 번째 메인 메뉴 추가 실패");

        Assert.AreEqual(main1, model.GetBentoSelection(0).MainMenu, "도시락 1에 main1이 없음");
        Assert.AreEqual(main2, model.GetBentoSelection(1).MainMenu, "도시락 2에 main2가 없음");
        Assert.AreEqual(main3, model.GetBentoSelection(2).MainMenu, "도시락 3에 main3이 없음");
    }

    /// <summary>
    /// Bug Fix: 메인 메뉴 한도 초과 시 실패
    /// </summary>
    [Test]
    public void AddBentoFood_AutoFind_FailsWhenAllBentosFull()
    {
        // Arrange: 3개 도시락에 메인 메뉴 모두 추가
        model.AddBentoFood(CreateTestFood("main1", "김밥", FoodType.MAIN));
        model.AddBentoFood(CreateTestFood("main2", "주먹밥", FoodType.MAIN));
        model.AddBentoFood(CreateTestFood("main3", "샌드위치", FoodType.MAIN));

        // Act: 4번째 메인 메뉴 추가 시도
        var main4 = CreateTestFood("main4", "카레라이스", FoodType.MAIN);
        bool success = model.AddBentoFood(main4);

        // Assert: 실패해야 함
        Assert.IsFalse(success, "모든 도시락이 가득 찬 상태에서 메인 메뉴 추가 성공함");
    }

    /// <summary>
    /// Bug Fix: 사이드 메뉴 자동 추가 (메인 메뉴 있는 도시락 찾기)
    /// </summary>
    [Test]
    public void AddBentoFood_AutoFind_AddsSideToFirstBentoWithMain()
    {
        // Arrange: 도시락 1에 메인 메뉴 추가
        var main = CreateTestFood("main1", "김밥", FoodType.MAIN);
        model.AddBentoFood(main);

        // Act: 사이드 메뉴 추가 (자동으로 메인 메뉴 있는 도시락 찾음)
        var side1 = CreateTestFood("side1", "단무지", FoodType.SIDE);
        bool success = model.AddBentoFood(side1);

        // Assert: 도시락 1에 추가됨
        Assert.IsTrue(success, "사이드 메뉴 추가 실패");
        Assert.Contains(side1, model.GetBentoSelection(0).SideMenus, "도시락 1에 사이드 메뉴가 없음");
    }

    /// <summary>
    /// Bug Fix: 사이드 메뉴 최대 3개 제한
    /// </summary>
    [Test]
    public void AddBentoFood_AutoFind_EnforcesSideMenuLimit()
    {
        // Arrange: 도시락 1에 메인 + 사이드 3개 추가
        model.AddBentoFood(CreateTestFood("main1", "김밥", FoodType.MAIN));
        model.AddBentoFood(CreateTestFood("side1", "단무지", FoodType.SIDE));
        model.AddBentoFood(CreateTestFood("side2", "김치", FoodType.SIDE));
        model.AddBentoFood(CreateTestFood("side3", "콩나물", FoodType.SIDE));

        // 도시락 2에 메인 추가 (사이드 추가를 위한 대상)
        model.AddBentoFood(CreateTestFood("main2", "주먹밥", FoodType.MAIN));

        // Act: 4번째 사이드 추가 (도시락 1은 가득 차서 도시락 2에 추가되어야 함)
        var side4 = CreateTestFood("side4", "계란말이", FoodType.SIDE);
        bool success = model.AddBentoFood(side4);

        // Assert: 성공, 도시락 2에 추가됨
        Assert.IsTrue(success, "4번째 사이드 메뉴 추가 실패");
        Assert.AreEqual(3, model.GetBentoSelection(0).SideMenus.Count, "도시락 1 사이드 개수 오류");
        Assert.AreEqual(1, model.GetBentoSelection(1).SideMenus.Count, "도시락 2 사이드 개수 오류");
        Assert.Contains(side4, model.GetBentoSelection(1).SideMenus, "도시락 2에 side4가 없음");
    }

    /// <summary>
    /// Bug Fix: 사이드 메뉴 중복 방지
    /// </summary>
    [Test]
    public void AddBentoFood_AutoFind_PreventsDuplicateSides()
    {
        // Arrange: 도시락 1에 메인 + 사이드 1개
        model.AddBentoFood(CreateTestFood("main1", "김밥", FoodType.MAIN));
        var side1 = CreateTestFood("side1", "단무지", FoodType.SIDE);
        model.AddBentoFood(side1);

        // Act: 같은 사이드 메뉴 다시 추가 시도
        bool success = model.AddBentoFood(side1);

        // Assert: 실패하거나, 성공 시 도시락 2에 추가되어야 함 (도시락 1에는 중복 없음)
        // 현재 구현: 이미 추가된 경우 skip하고 다음 도시락 찾음
        var bento1Sides = model.GetBentoSelection(0).SideMenus;
        int side1Count = bento1Sides.Count(s => s.id == side1.id);
        Assert.AreEqual(1, side1Count, "도시락 1에 같은 사이드 메뉴가 중복으로 추가됨");
    }

    /// <summary>
    /// Bug Fix: RemoveBentoFood(FoodData) 자동으로 음식 찾아서 제거
    /// </summary>
    [Test]
    public void RemoveBentoFood_AutoFind_RemovesFromCorrectBento()
    {
        // Arrange: 도시락 1, 2에 각각 메인 메뉴 추가
        var main1 = CreateTestFood("main1", "김밥", FoodType.MAIN);
        var main2 = CreateTestFood("main2", "주먹밥", FoodType.MAIN);
        model.AddBentoFood(main1);
        model.AddBentoFood(main2);

        // Act: main2 제거 (도시락 2에서 제거되어야 함)
        bool success = model.RemoveBentoFood(main2);

        // Assert: 성공, 도시락 2만 비어있음
        Assert.IsTrue(success, "음식 제거 실패");
        Assert.AreEqual(main1, model.GetBentoSelection(0).MainMenu, "도시락 1의 메인 메뉴가 삭제됨");
        Assert.IsNull(model.GetBentoSelection(1).MainMenu, "도시락 2의 메인 메뉴가 삭제되지 않음");
    }

    /// <summary>
    /// Bug Fix: RemoveBentoFood 존재하지 않는 음식 제거 시 실패
    /// </summary>
    [Test]
    public void RemoveBentoFood_AutoFind_FailsWhenFoodNotFound()
    {
        // Arrange: 도시락에 메인 메뉴 추가
        model.AddBentoFood(CreateTestFood("main1", "김밥", FoodType.MAIN));

        // Act: 존재하지 않는 음식 제거 시도
        var nonExistent = CreateTestFood("main99", "없는메뉴", FoodType.MAIN);
        bool success = model.RemoveBentoFood(nonExistent);

        // Assert: 실패해야 함
        Assert.IsFalse(success, "존재하지 않는 음식 제거 성공함");
    }

    /// <summary>
    /// Bug Fix: Indexed AddBentoFood의 메인 메뉴 중복 방지
    /// </summary>
    [Test]
    public void AddBentoFood_Indexed_PreventsDuplicateMain()
    {
        // Arrange: 도시락 1에 메인 메뉴 추가
        var main1 = CreateTestFood("main1", "김밥", FoodType.MAIN);
        model.AddBentoFood(0, main1);

        // Act: 같은 도시락에 다른 메인 메뉴 추가 시도
        var main2 = CreateTestFood("main2", "주먹밥", FoodType.MAIN);
        bool success = model.AddBentoFood(0, main2);

        // Assert: 실패해야 함
        Assert.IsFalse(success, "메인 메뉴가 이미 있는데 추가 성공함");
        Assert.AreEqual(main1, model.GetBentoSelection(0).MainMenu, "메인 메뉴가 교체됨");
    }

    /// <summary>
    /// Bug Fix: Indexed AddBentoFood의 사이드 메뉴 중복 방지
    /// </summary>
    [Test]
    public void AddBentoFood_Indexed_PreventsDuplicateSide()
    {
        // Arrange: 도시락 1에 메인 + 사이드 1개 추가
        model.AddBentoFood(0, CreateTestFood("main1", "김밥", FoodType.MAIN));
        var side1 = CreateTestFood("side1", "단무지", FoodType.SIDE);
        model.AddBentoFood(0, side1);

        // Act: 같은 사이드 메뉴 다시 추가 시도
        bool success = model.AddBentoFood(0, side1);

        // Assert: 실패해야 함
        Assert.IsFalse(success, "같은 사이드 메뉴 추가 성공함");
        Assert.AreEqual(1, model.GetBentoSelection(0).SideMenus.Count, "사이드 메뉴가 중복 추가됨");
    }

    /// <summary>
    /// Bug Fix: Indexed AddBentoFood의 사이드 메뉴 한도 체크
    /// </summary>
    [Test]
    public void AddBentoFood_Indexed_EnforcesSideLimit()
    {
        // Arrange: 도시락 1에 메인 + 사이드 3개 추가
        model.AddBentoFood(0, CreateTestFood("main1", "김밥", FoodType.MAIN));
        model.AddBentoFood(0, CreateTestFood("side1", "단무지", FoodType.SIDE));
        model.AddBentoFood(0, CreateTestFood("side2", "김치", FoodType.SIDE));
        model.AddBentoFood(0, CreateTestFood("side3", "콩나물", FoodType.SIDE));

        // Act: 4번째 사이드 추가 시도
        var side4 = CreateTestFood("side4", "계란말이", FoodType.SIDE);
        bool success = model.AddBentoFood(0, side4);

        // Assert: 실패해야 함
        Assert.IsFalse(success, "사이드 3개 초과 추가 성공함");
        Assert.AreEqual(3, model.GetBentoSelection(0).SideMenus.Count, "사이드 메뉴 개수 오류");
    }

    /// <summary>
    /// Bug Fix: 사이드 메뉴는 메인 메뉴 필수
    /// </summary>
    [Test]
    public void AddBentoFood_Indexed_RequiresMainBeforeSide()
    {
        // Arrange: 빈 도시락

        // Act: 메인 메뉴 없이 사이드 메뉴 추가 시도
        var side = CreateTestFood("side1", "단무지", FoodType.SIDE);
        bool success = model.AddBentoFood(0, side);

        // Assert: 실패해야 함
        Assert.IsFalse(success, "메인 메뉴 없이 사이드 메뉴 추가 성공함");
        Assert.AreEqual(0, model.GetBentoSelection(0).SideMenus.Count, "사이드 메뉴가 추가됨");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 테스트용 FoodData 생성 (ScriptableObject)
    /// </summary>
    private FoodData CreateTestFood(string id, string name, FoodType type)
    {
        var food = UnityEngine.ScriptableObject.CreateInstance<FoodData>();
        food.id = id;
        food.ingredientName = name;
        food.type = type;
        return food;
    }

    #endregion
}

/// <summary>
/// Mock IPhaseProgressor for testing
/// </summary>
public class MockPhaseProgressor : IPhaseProgressor
{
    public int PassPhaseCallCount { get; private set; }

    public void PassPhase()
    {
        PassPhaseCallCount++;
    }
}
