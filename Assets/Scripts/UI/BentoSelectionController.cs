using System.Collections.Generic;
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

    private void Start()
    {
        LoadAllFoodData();
        InitializeController();
    }

    private void LoadAllFoodData()
    {
        // UnlockedFoodManager가 있으면 해금된 레시피만 로드
        if (UnlockedFoodManager.Instance != null)
        {
            unlockedProvider = UnlockedFoodManager.Instance;
            var unlockedMains = unlockedProvider.GetUnlockedMainFoods();
            var unlockedSides = unlockedProvider.GetUnlockedSideFoods();

            allFoodData = new List<FoodData>();
            allFoodData.AddRange(unlockedMains);
            allFoodData.AddRange(unlockedSides);

            Debug.Log($"[BentoSelection] Loaded {unlockedMains.Count} main + {unlockedSides.Count} side = {allFoodData.Count} unlocked recipes");
        }
        else
        {
            // UnlockedFoodManager가 없으면 모든 레시피 로드 (폴백)
            Debug.LogWarning("[BentoSelection] UnlockedFoodManager not found, loading all recipes");
            FoodData[] foods = Resources.LoadAll<FoodData>("ScriptableObjects/FoodData");
            allFoodData = new List<FoodData>(foods);
            Debug.Log($"[BentoSelection] Loaded {allFoodData.Count} food data assets (all recipes)");
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
        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
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
            else
                Debug.Log("[BentoSelection] Max 3 sides allowed.");
        }

        RefreshSlotUI(bentoIndex);
    }

    private void RefreshSlotUI(int bentoIndex)
    {
        var slot = bentoSlots[bentoIndex];
        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
        if (slot == null || menu == null) return;

        slot.Refresh(menu, menu.MainMenu, menu.SideMenus);
    }

    private void OnBentoNameChanged(int index, string newName)
    {
        var menu = RecipeDataManager.Instance.GetMenu(index);
        if (menu != null)
        {
            menu.Name = newName;
            Debug.Log($"[BentoSelection] Bento {index} renamed to: {newName}");
        }
    }

    public void ConfirmAll()
    {
        if (!RecipeDataManager.Instance.HasAnySelection())
        {
            Debug.LogWarning("[BentoSelection] No menus selected!");
            return;
        }

        onConfirmCallback?.Invoke();
        gameObject.SetActive(false);
        UILockManager.Unlock(UILockManager.Owner.BentoSelection);
    }

    public void Show(System.Action onConfirm)
    {
        UILockManager.Lock(UILockManager.Owner.BentoSelection);
        onConfirmCallback = onConfirm;
        gameObject.SetActive(true);
        InitializeController();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        onConfirmCallback = null;
        UILockManager.Unlock(UILockManager.Owner.BentoSelection);
    }
}
