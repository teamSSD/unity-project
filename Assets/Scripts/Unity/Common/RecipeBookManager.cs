using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RecipeBookManager : SingletonMonoBehaviour<RecipeBookManager>
{
    public const int CanvasSortingOrder = 1100;

    public static bool HasInstance => Instance != null;

    private static bool isRecipeBookActive = false;
    public static bool IsRecipeBookActive => isRecipeBookActive;
    public static bool IsReadOnly => true;

    /// <summary>튜토리얼이 강제로 열린 상태 유지 (Tab/ESC/X 버튼 무시). 카드 닫기는 여전히 가능.
    /// Setter가 X(closeButton) interactable도 함께 토글.</summary>
    private static bool _tutorialForceOpen;
    public static bool TutorialForceOpen
    {
        get => _tutorialForceOpen;
        set
        {
            _tutorialForceOpen = value;
            if (Instance != null && Instance.closeButton != null)
                Instance.closeButton.interactable = !value;
        }
    }

    /// <summary>Tab으로 책 오픈 차단. 레시피북 안내 이전 튜토리얼 파트들에서 사용.</summary>
    public static bool TutorialBlockOpen { get; set; }

    [Header("Recipe Book Root")]
    [SerializeField] public GameObject bookRoot;

    [Header("Inventory Page")]
    [SerializeField] public GameObject inventory;

    [Header("Menu Page (Main L / Side R)")]
    [SerializeField] public GameObject mainMenu;

    [Header("Menu Card")]
    [SerializeField] public GameObject menuCard;

    [Header("Card Instantiate Transform (L)")]
    [SerializeField] public Transform cardInstantiateTransform_L;

    [Header("Card Instantiate Transform (R)")]
    [SerializeField] public Transform cardInstantiateTransform_R;

    [Header("Recipe Containers (L=Main, R=Side)")]
    [SerializeField] private Transform mainRecipeL;
    [SerializeField] private Transform mainRecipeR;

    [Header("Chrome")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button menuBookmarkButton;
    [SerializeField] private Button inventoryBookmarkButton;

    private Canvas canvas;
    private MenuCardOverlay cardOverlay;
    private InventoryPageController inventoryPageController;

    // 북마크 RT (선택 시 가로 늘림) — 인스펙터 노출 대신 Awake에서 Button 참조로 GetComponent.
    private RectTransform menuBookmarkRT;
    private RectTransform inventoryBookmarkRT;
    private const float BookmarkWidthUnselected = 95f;
    private const float BookmarkWidthSelected   = 105f;
    private const float BookmarkHeight          = 45f;

    private enum Page { Inventory, Menu }
    private Page lastPage = Page.Menu;

    protected override void OnSingletonAwake()
    {
        if (transform.parent != null)
            transform.SetParent(null);

        var wrapper = new GameObject("RecipeBookCanvas");
        canvas = wrapper.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // TutorialOverlayCanvas(1000)보다 위 — 레시피북 열림 시 튜토리얼 말풍선이
        // 창을 관통해 보이던 문제 fix. MenuCard 등 자식 UI도 자동 상속.
        canvas.sortingOrder = CanvasSortingOrder;

        var scaler = wrapper.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        wrapper.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        transform.SetParent(wrapper.transform, false);

        cardOverlay = gameObject.GetComponent<MenuCardOverlay>();
        if (cardOverlay == null) cardOverlay = gameObject.AddComponent<MenuCardOverlay>();

        inventoryPageController = inventory.GetComponentInChildren<InventoryPageController>(true);

        // 북마크 RT 캐시 + 클릭 와이어. 인스펙터에서 wire된 Button 기준.
        if (menuBookmarkButton != null)
        {
            menuBookmarkRT = menuBookmarkButton.GetComponent<RectTransform>();
            menuBookmarkButton.onClick.AddListener(OpenMenu);
        }
        if (inventoryBookmarkButton != null)
        {
            inventoryBookmarkRT = inventoryBookmarkButton.GetComponent<RectTransform>();
            inventoryBookmarkButton.onClick.AddListener(OpenInventory);
        }

        isRecipeBookActive = false;
        canvas.enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isRecipeBookActive)
            {
                if (!TutorialForceOpen) Close(); // 튜토리얼 강제 유지 중엔 Tab으로 안 닫힘.
            }
            else if (!TutorialBlockOpen &&
                     (UILockManager.CanOpen(UILockManager.Owner.RecipeBook)
                      || UILockManager.IsLockedBy(UILockManager.Owner.CookingTutorial)))
                Open();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isRecipeBookActive && !ConfirmModal.IsOpen)
        {
            if (MenuCardController.Instance != null)
                CloseMenuCard();
            else if (!TutorialForceOpen)
                Close(); // 튜토리얼 강제 유지 중엔 ESC로도 안 닫힘 (카드는 위 branch에서 여전히 닫힘).
        }
    }

    public void OpenRecipeBook(bool active)
    {
        if (!UILockManager.CanOpen(UILockManager.Owner.RecipeBook) &&
            !UILockManager.IsLockedBy(UILockManager.Owner.CookingTutorial))
            return;

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(active);
            // 튜토리얼 forceOpen 중이면 X 눌러도 안 닫히게 interactable=false.
            closeButton.interactable = !TutorialForceOpen;
        }

        if (isRecipeBookActive)
        {
            Debug.LogWarning("RecipeBook is already open.");
            return;
        }

        SoundManager.Instance?.PlayUIBook();
        SoundManager.Instance?.RegisterButtons(transform);

        switch (lastPage)
        {
            case Page.Inventory: OpenInventory(); break;
            default: OpenMenu(); break;
        }

        canvas.enabled = true;
        Canvas.ForceUpdateCanvases();

        if (!string.IsNullOrEmpty(cardOverlay.LastCardFoodId))
            OpenMenuCardL(cardOverlay.LastCardFoodId);

        UILockManager.Lock(UILockManager.Owner.RecipeBook);
        isRecipeBookActive = true;
    }

    /// <summary>레시피북이 실제로 닫힌 직후 발화 (카드 닫힘과 구분). 튜토리얼 등이 훅.</summary>
    public event System.Action OnRecipeBookClosed;

    public void CloseRecipeBook()
    {
        if (MenuCardController.Instance != null)
            cardOverlay.Close();

        canvas.enabled = false;
        isRecipeBookActive = false;
        UILockManager.Unlock(UILockManager.Owner.RecipeBook);
        OnRecipeBookClosed?.Invoke();
    }

    private void ShowOnlyPage(GameObject page)
    {
        SetPageContent(inventory, page == inventory);
        SetPageContent(mainMenu, page == mainMenu);
    }

    private void UpdateBookmarkSelection(Page selected)
    {
        if (menuBookmarkRT != null)
            menuBookmarkRT.sizeDelta = new Vector2(
                selected == Page.Menu ? BookmarkWidthSelected : BookmarkWidthUnselected,
                BookmarkHeight);
        if (inventoryBookmarkRT != null)
            inventoryBookmarkRT.sizeDelta = new Vector2(
                selected == Page.Inventory ? BookmarkWidthSelected : BookmarkWidthUnselected,
                BookmarkHeight);
    }

    private void SetPageContent(GameObject panel, bool active)
    {
        // 북마크가 더 이상 안에 없음 → 모든 자식 토글
        for (int i = 0; i < panel.transform.childCount; i++)
            panel.transform.GetChild(i).gameObject.SetActive(active);
    }

    public void OpenInventory()
    {
        lastPage = Page.Inventory;
        ShowOnlyPage(inventory);
        UpdateBookmarkSelection(Page.Inventory);
        CloseMenuCard();
        inventoryPageController?.Refresh();
    }

    public void OpenMenu()
    {
        lastPage = Page.Menu;
        ShowOnlyPage(mainMenu);
        UpdateBookmarkSelection(Page.Menu);
        CloseMenuCard();
        if (GameSessionRoot.Instance?.UnlockedFood != null)
        {
            PopulateContainer(mainRecipeL, GameSessionRoot.Instance?.UnlockedFood.GetAllMainFoods());
            PopulateContainer(mainRecipeR, GameSessionRoot.Instance?.UnlockedFood.GetAllSideFoods());
        }
    }

    private GameObject menuSlotTemplate;

    private void EnsureMenuSlotTemplate(Transform container)
    {
        if (menuSlotTemplate != null) return;
        var first = container.GetComponentInChildren<MenuSlot>(true);
        if (first == null) return;
        menuSlotTemplate = Instantiate(first.gameObject, transform);
        menuSlotTemplate.SetActive(false);
        menuSlotTemplate.name = "MenuSlot_Template";
    }

    private void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void PopulateContainer(Transform container, List<FoodData> foods)
    {
        EnsureMenuSlotTemplate(container);
        ClearChildren(container);
        var unlockedMgr = GameSessionRoot.Instance?.UnlockedFood;
        var sorted = unlockedMgr != null
            ? foods.OrderByDescending(f => unlockedMgr.IsUnlocked(f.id))
            : (IEnumerable<FoodData>)foods;
        foreach (var food in sorted)
        {
            var go = Instantiate(menuSlotTemplate, container);
            go.SetActive(true);
            var slot = go.GetComponent<MenuSlot>();
            slot.Id = food.id;
            if (unlockedMgr != null && unlockedMgr.IsUnlocked(food.id))
                slot.InitSlot();
            else
                slot.InitEmpty();
        }
    }

    public void OpenMenuCardL(string id)
    {
        cardOverlay.Open(id, bookRoot.transform, menuCard);
    }

    public void OpenMenuCardR(string id)
    {
        cardOverlay.Open(id, bookRoot.transform, menuCard);
    }

    public void CloseMenuCard()
    {
        cardOverlay.Close();
    }

    public void Open()
    {
        OpenRecipeBook(true);
    }

    public void Close()
    {
        CloseRecipeBook();
    }

#if UNITY_EDITOR
    private void OnValidate() => RequiredFieldValidator.Validate(this);
#endif
}
