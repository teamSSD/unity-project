using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RecipeBookManager : SingletonMonoBehaviour<RecipeBookManager>
{
    public static bool HasInstance => Instance != null;

    private static bool isRecipeBookActive = false;
    public static bool IsRecipeBookActive => isRecipeBookActive;
    public static bool IsReadOnly => true;

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

    private Canvas canvas;
    private MenuCardOverlay cardOverlay;
    private Button closeButton;
    private InventoryPageController inventoryPageController;

    // 북마크 RT (선택 시 가로 늘림)
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
        canvas.sortingOrder = 100;

        var scaler = wrapper.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        wrapper.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        transform.SetParent(wrapper.transform, false);

        cardOverlay = gameObject.GetComponent<MenuCardOverlay>();
        if (cardOverlay == null) cardOverlay = gameObject.AddComponent<MenuCardOverlay>();

        closeButton = bookRoot.transform.Find("Button_Close")?.GetComponentInChildren<Button>();
        inventoryPageController = inventory.GetComponentInChildren<InventoryPageController>(true);

        // 북마크는 페이지 밖(Page 직속 형제). 클릭 와이어도 여기서.
        var page = bookRoot.transform.Find("Page");
        var menuBM = page?.Find("MainRecipe_BookMark");
        var invBM  = page?.Find("Inventory_BookMark");
        if (menuBM != null)
        {
            menuBookmarkRT = menuBM.GetComponent<RectTransform>();
            var btn = menuBM.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OpenMenu);
        }
        if (invBM != null)
        {
            inventoryBookmarkRT = invBM.GetComponent<RectTransform>();
            var btn = invBM.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(OpenInventory);
        }

        isRecipeBookActive = false;
        canvas.enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isRecipeBookActive)
                Close();
            else if (UILockManager.CanOpen(UILockManager.Owner.RecipeBook))
                Open();
        }

        if (Input.GetKeyDown(KeyCode.Escape) && isRecipeBookActive && !ConfirmModal.IsOpen)
        {
            if (MenuCardController.Instance != null)
                CloseMenuCard();
            else
                Close();
        }
    }

    public void OpenRecipeBook(bool active)
    {
        if (closeButton != null) closeButton.gameObject.SetActive(active);

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

    public void CloseRecipeBook()
    {
        if (MenuCardController.Instance != null)
            cardOverlay.Close();

        canvas.enabled = false;
        isRecipeBookActive = false;
        UILockManager.Unlock(UILockManager.Owner.RecipeBook);
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
