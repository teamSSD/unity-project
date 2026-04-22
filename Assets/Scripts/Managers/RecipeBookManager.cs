using System.Collections.Generic;
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

    [Header("Diary Page")]
    [SerializeField] public GameObject diary;

    [Header("Diary Menu Slot")]
    [SerializeField] public GameObject menuSlot;

    [Header("Main Menu Page")]
    [SerializeField] public GameObject mainMenu;

    [Header("Side Menu Page")]
    [SerializeField] public GameObject sideMenu;

    [Header("Menu Card")]
    [SerializeField] public GameObject menuCard;

    [Header("Card Instantiate Transform (L)")]
    [SerializeField] public Transform cardInstantiateTransform_L;

    [Header("Card Instantiate Transform (R)")]
    [SerializeField] public Transform cardInstantiateTransform_R;

    [Header("Main Recipe Containers")]
    [SerializeField] private Transform mainRecipeL;
    [SerializeField] private Transform mainRecipeR;

    [Header("Side Recipe Containers")]
    [SerializeField] private Transform sideRecipeL;
    [SerializeField] private Transform sideRecipeR;

    private Canvas canvas;
    private MenuCardOverlay cardOverlay;
    private Button closeButton;
    private DiaryMenuDisplay diaryMenuDisplay;
    private Transform diaryPageL;

    // 세션 내 상태 기억
    private enum Page { Diary, Main, Side }
    private Page lastPage = Page.Diary;

    protected override void OnSingletonAwake()
    {
        if (transform.parent != null)
            transform.SetParent(null);

        // 래퍼 Canvas 생성 (RecipeBook은 자식으로 원래 크기 1320x870 유지)
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
        diaryMenuDisplay = diary.GetComponentInChildren<DiaryMenuDisplay>(true);
        diaryPageL = diary.transform.Find("Page_L");

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

        if (Input.GetKeyDown(KeyCode.Escape) && isRecipeBookActive)
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

        canvas.enabled = true;
        foreach (MenuSlot slot in GetComponentsInChildren<MenuSlot>())
            slot.InitSlot();
        UpdateDiaryCheckmarks();

        // 마지막 페이지 복원
        switch (lastPage)
        {
            case Page.Main: OpenMainMenu(); break;
            case Page.Side: OpenSideMenu(); break;
            default: OpenDiary(); break;
        }

        // 마지막에 열려있던 카드 복원
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
        SetPageContent(diary, page == diary);
        SetPageContent(mainMenu, page == mainMenu);
        SetPageContent(sideMenu, page == sideMenu);
    }

    // Child 0 is the bookmark tab button — always stays visible
    private void SetPageContent(GameObject panel, bool active)
    {
        for (int i = 1; i < panel.transform.childCount; i++)
            panel.transform.GetChild(i).gameObject.SetActive(active);
    }

    public void OpenDiary()
    {
        lastPage = Page.Diary;
        ShowOnlyPage(diary);
        CloseMenuCard();

        if (diaryMenuDisplay != null) diaryMenuDisplay.Refresh();
    }
    public void OpenMainMenu()
    {
        lastPage = Page.Main;
        ShowOnlyPage(mainMenu);
        CloseMenuCard();
        if (UnlockedFoodManager.Instance != null)
            PopulateRecipePage(mainRecipeL, mainRecipeR,
                UnlockedFoodManager.Instance.GetUnlockedMainFoods());
    }
    public void OpenSideMenu()
    {
        lastPage = Page.Side;
        ShowOnlyPage(sideMenu);
        CloseMenuCard();
        if (UnlockedFoodManager.Instance != null)
            PopulateRecipePage(sideRecipeL, sideRecipeR,
                UnlockedFoodManager.Instance.GetUnlockedSideFoods());
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

    private void PopulateRecipePage(Transform containerL, Transform containerR, List<FoodData> foods)
    {
        EnsureMenuSlotTemplate(containerL);

        ClearChildren(containerL);
        ClearChildren(containerR);

        for (int i = 0; i < foods.Count; i++)
        {
            var parent = (i < 6) ? containerL : containerR;
            var go = Instantiate(menuSlotTemplate, parent);
            go.SetActive(true);
            var slot = go.GetComponent<MenuSlot>();
            slot.Id = foods[i].id;
            slot.InitSlot();
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

    private static readonly string[] DiaryTimeSections = { "Morning", "Lunch", "Evening", "Night" };

    private void UpdateDiaryCheckmarks()
    {
        if (ActionSelectionManager.Instance == null || diary == null) return;

        if (diaryPageL == null) return;

        for (int i = 0; i < DiaryTimeSections.Length; i++)
        {
            Transform section = diaryPageL.Find(DiaryTimeSections[i]);
            if (section == null) continue;

            var actionToggles = section.GetComponentsInChildren<ActionToggle>(true);
            var saved = ActionSelectionManager.Instance.GetAction(i);
            int savedType = saved != null && saved.HasSelection() ? (int)saved.SelectedAction : 0;

            foreach (var at in actionToggles)
            {
                bool isSelected = savedType != 0 && at.actionType == savedType;
                if (at.checkmark != null)
                    at.checkmark.SetActive(isSelected);
                if (at.toggle != null)
                    at.toggle.SetIsOnWithoutNotify(isSelected);
            }
        }
    }

    /// <summary>
    /// 레시피북 열기 (읽기 전용)
    /// </summary>
    public void Open()
    {
        OpenRecipeBook(true);
    }

    /// <summary>
    /// 레시피북 닫기
    /// </summary>
    public void Close()
    {
        CloseRecipeBook();
    }
}
