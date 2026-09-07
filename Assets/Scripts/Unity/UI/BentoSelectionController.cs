using System.Collections.Generic;
using Game.Domain.Cooking;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Orchestrates the Bento Selection UI.
/// Coordinates data flow between RecipeDataManager and individual BentoSlotUI components.
/// </summary>
public class BentoSelectionController : MonoBehaviour
{
    [Header("Bento Slots")]
    public BentoSlotUI[] bentoSlots; // 0: Bento 1, 1: Bento 2, 2: Bento 3
    
    [Header("Prefabs")]
    public GameObject itemPrefab;

    [Header("Global UI")]
    public Button backButton;
    public Button confirmButton;

    private System.Action onConfirmCallback;
    private List<FoodData> allFoodData = new List<FoodData>();
    private IUnlockedFoodProvider unlockedProvider;
    private MenuSelectionService menuAccess;

    /// <summary>Composition Root에서 의존 명시 주입 — Start에서의 singleton 직접 조회 제거.</summary>
    public void Inject(IUnlockedFoodProvider unlocked, MenuSelectionService menu)
    {
        unlockedProvider = unlocked;
        menuAccess = menu;
        LoadAllFoodData();
        InitializeController();
    }

    private void LoadAllFoodData()
    {
        if (unlockedProvider != null)
        {
            allFoodData = new List<FoodData>();
            allFoodData.AddRange(unlockedProvider.GetUnlockedMainFoods());
            allFoodData.AddRange(unlockedProvider.GetUnlockedSideFoods());
        }
        else
        {
            Debug.LogWarning("[BentoSelection] No unlocked food provider injected, loading all recipes");
            allFoodData = new List<FoodData>(CatalogProvider.Food?.All ?? new List<FoodData>());
        }
    }

    private void InitializeController()
    {
        for (int i = 0; i < bentoSlots.Length; i++)
        {
            if (bentoSlots[i] == null) continue;
            
            int bentoIndex = i;
            bentoSlots[i].Initialize(bentoIndex, OnBentoNameChanged);

            // Instantiate items for each slot
            PopulateSlotItems(bentoIndex);
            RefreshSlotUI(bentoIndex);
        }

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmAll);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(Close);
        }
    }

    private void PopulateSlotItems(int bentoIndex)
    {
        var slot = bentoSlots[bentoIndex];
        if (slot == null || itemPrefab == null) return;

        slot.mainCategory.Clear();
        slot.sideCategory.Clear();

        foreach (var food in allFoodData)
        {
            if (food.type == FoodType.MAIN)
            {
                slot.mainCategory.AddItem(itemPrefab, food, (item) => HandleItemSelection(bentoIndex, item, true));
            }
            else if (food.type == FoodType.SIDE)
            {
                slot.sideCategory.AddItem(itemPrefab, food, (item) => HandleItemSelection(bentoIndex, item, false));
            }
        }
    }

    private void HandleItemSelection(int bentoIndex, MenuSelectionItem item, bool isMain)
    {
        var menu = menuAccess?.GetMenu(bentoIndex);
        if (menu == null || item.CurrentFood == null) return;

        if (isMain)
        {
            if (menu.MainMenu == item.CurrentFood)
                menu.SetMain(null);
            else
                menu.SetMain(item.CurrentFood);
        }
        else
        {
            if (menu.SideMenus.Contains(item.CurrentFood))
                menu.RemoveSide(item.CurrentFood);
            else if (menu.SideMenus.Count < 3)
                menu.AddSide(item.CurrentFood);
        }

        RefreshSlotUI(bentoIndex);
    }

    private void RefreshSlotUI(int bentoIndex)
    {
        var slot = bentoSlots[bentoIndex];
        var menu = menuAccess?.GetMenu(bentoIndex);
        if (slot == null || menu == null) return;

        slot.Refresh(menu, menu.MainMenu, menu.SideMenus);
    }

    private void OnBentoNameChanged(int index, string newName)
    {
        var menu = menuAccess?.GetMenu(index);
        if (menu != null)
        {
            menu.Name = newName;
        }
    }

    public void ConfirmAll()
    {
        // 규칙:
        // - side만 있는 슬롯 존재 → 팝업 후 차단 (도시락 성립 불가)
        // - 전부 빈 상태 → 팝업 후 차단 (최소 1개 도시락 필요)
        // - 완전히 빈 슬롯 / main만 있는 슬롯 / main+side 슬롯 → 허용
        var invalidIndices = menuAccess?.GetInvalidSlotIndices();
        if (invalidIndices != null && invalidIndices.Count > 0)
        {
            var names = new List<string>();
            foreach (int idx in invalidIndices)
            {
                var menu = menuAccess.GetMenu(idx);
                names.Add(menu?.Name ?? $"도시락 {idx + 1}");
            }
            ConfirmModal.Alert(
                title: "메뉴 확인 필요",
                message: $"{string.Join(", ", names)}에 메인 메뉴가 없습니다.\n사이드 메뉴만으로는 도시락을 만들 수 없어요."
            );
            return;
        }

        if (!(menuAccess?.HasAnySelection() ?? false))
        {
            ConfirmModal.Alert(
                title: "메뉴 확인 필요",
                message: "최소 하나의 도시락을 선택해주세요."
            );
            return;
        }

        onConfirmCallback?.Invoke();
        gameObject.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.BentoSelection);
    }

    public void Show(System.Action onConfirm)
    {
        if (!UILockManager.CanOpen(UILockManager.Owner.BentoSelection)) return;

        UILockManager.Lock(UILockManager.Owner.BentoSelection);
        onConfirmCallback = onConfirm;
        gameObject.SetActive(true);
        SoundManager.Instance?.PlayUIBook();
        SoundManager.Instance?.RegisterButtons(transform);
        // 신규 unlock(퀘스트 수락 후 추가된 메뉴) 반영을 위해 매번 갱신
        LoadAllFoodData();
        InitializeController();

        TryShowMenuSelectionTutorial();
    }

    private void TryShowMenuSelectionTutorial()
    {
        var tc = TutorialController.Instance;
        if (tc == null || !tc.CanShow(TutorialStepId.MenuSelection)) return;

        // 튜토리얼 중 BentoSelection 상호작용 완전 차단 (버튼/ESC).
        var cg = GetOrAddCanvasGroup();
        cg.interactable = false;
        cg.blocksRaycasts = false;

        StartCoroutine(ShowNextFrame(tc, cg));
    }

    private System.Collections.IEnumerator ShowNextFrame(TutorialController tc, CanvasGroup cg)
    {
        // Layout이 실제 크기로 rebuild될 때까지 몇 프레임 대기 (BentoSlot 동적 생성 대응).
        yield return null;
        yield return null;
        tc.Show(TutorialStepId.MenuSelection, onDone: () =>
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
        });
    }

    private CanvasGroup GetOrAddCanvasGroup()
    {
        var cg = GetComponent<CanvasGroup>();
        if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
        return cg;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 튜토리얼 활성 중엔 임의 취소 금지.
            var tc = TutorialController.Instance;
            if (tc != null && !tc.IsCompleted) return;
            Close();
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
        onConfirmCallback = null;
        UILockManager.Unlock(UILockManager.Owner.BentoSelection);
    }
}
