using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ResourcePaths;

public class RecipeBookManager : MonoBehaviour
{
    private static RecipeBookManager instance;

    /// <summary>
    /// RecipeBookManager 인스턴스가 존재하는지 확인 (에러 로그 없음)
    /// </summary>
    public static bool HasInstance
    {
        get
        {
            if (instance != null) return true;
            instance = FindFirstObjectByType<RecipeBookManager>();
            return instance != null;
        }
    }

    public static RecipeBookManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<RecipeBookManager>();
                if (instance == null)
                {
                    Debug.LogError("RecipeBookManager instance not found in scene.");
                }
            }
            return instance;
        }
    }

    private static bool isRecipeBookActive = false;
    public static bool IsRecipeBookActive
    {
        get { return isRecipeBookActive; }
    }

    // RecipeBook은 이제 항상 읽기 전용 (View Mode만 지원)
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
    private GameObject overlay;

    // 세션 내 상태 기억
    private enum Page { Diary, Main, Side }
    private Page lastPage = Page.Diary;
    private string lastCardFoodId;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

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
        DontDestroyOnLoad(wrapper);

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
        bookRoot.transform.Find("Button_Close").GetComponentInChildren<Button>().gameObject.SetActive(active);

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
        if (!string.IsNullOrEmpty(lastCardFoodId))
            OpenMenuCardL(lastCardFoodId);

        UILockManager.Lock(UILockManager.Owner.RecipeBook);
        isRecipeBookActive = true;
    }
    public void CloseRecipeBook()
    {
        // 카드가 열려있으면 foodId는 보존하고 오브젝트만 정리
        if (MenuCardController.Instance != null)
        {
            MenuCardController.Instance.CloseMenuCard();
            if (overlay != null) { Destroy(overlay); overlay = null; }
        }

        canvas.enabled = false;
        isRecipeBookActive = false;
        UILockManager.Unlock(UILockManager.Owner.RecipeBook);
    }
    public void OpenDiary()
    {
        lastPage = Page.Diary;
        diary.transform.SetAsLastSibling();
        CloseMenuCard();

        var display = diary.GetComponentInChildren<DiaryMenuDisplay>(true);
        if (display != null) display.Refresh();
    }
    public void OpenMainMenu()
    {
        lastPage = Page.Main;
        mainMenu.transform.SetAsLastSibling();
        CloseMenuCard();
        if (UnlockedFoodManager.Instance != null)
            PopulateRecipePage(mainRecipeL, mainRecipeR,
                UnlockedFoodManager.Instance.GetUnlockedMainFoods());
    }
    public void OpenSideMenu()
    {
        lastPage = Page.Side;
        sideMenu.transform.SetAsLastSibling();
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
        OpenMenuCard(id, cardInstantiateTransform_L);
    }
    public void OpenMenuCardR(string id)
    {
        OpenMenuCard(id, cardInstantiateTransform_R);
    }

    public void OpenMenuCard(string id, Transform form)
    {
        lastCardFoodId = id;

        if (MenuCardController.Instance != null)
        {
            MenuCardController.Instance.ClearMenuCard();
            MenuCardController.Instance.InitSlot(id);
            return;
        }

        // 반투명 오버레이 생성 (레시피북 위에 회색 깔기)
        overlay = new GameObject("MenuCardOverlay");
        overlay.transform.SetParent(bookRoot.transform, false);
        var overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        var overlayBtn = overlay.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        overlayBtn.onClick.AddListener(CloseMenuCard);
        overlay.transform.SetAsLastSibling();

        // MenuCard를 오버레이 위에 중앙 배치
        var cardGO = Instantiate(menuCard, overlay.transform);
        var cardRect = cardGO.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(500, 850);

        cardGO.GetComponent<MenuCardController>().InitSlot(id);

        // X 닫기 버튼 (카드 우상단, overlay 자식으로 absolute 배치)
        CreateCloseButton(overlay.transform, cardRect);
    }

    private void CreateCloseButton(Transform parent, RectTransform cardRect)
    {
        var btnGO = new GameObject("Button_Close");
        btnGO.transform.SetParent(parent, false);

        var rect = btnGO.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        // 카드 우상단 모서리에 배치
        float cardW = cardRect.sizeDelta.x;
        float cardH = cardRect.sizeDelta.y;
        rect.anchoredPosition = new Vector2(cardW / 2 - 15, cardH / 2 - 15);
        rect.sizeDelta = new Vector2(50, 50);

        var btn = btnGO.AddComponent<Button>();
        btnGO.AddComponent<Image>().color = new Color(0, 0, 0, 0);

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(btnGO.transform, false);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = "X";
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.2f, 0.2f, 0.2f);
        tmp.alignment = TextAlignmentOptions.Center;

        btn.onClick.AddListener(CloseMenuCard);
    }
    public void CloseMenuCard()
    {
        lastCardFoodId = null;

        if (MenuCardController.Instance != null)
        {
            MenuCardController.Instance.CloseMenuCard();
        }
        if (overlay != null)
        {
            Destroy(overlay);
            overlay = null;
        }
    }

    private static readonly string[] DiaryTimeSections = { "Morning", "Lunch", "Evening", "Night" };

    private void UpdateDiaryCheckmarks()
    {
        if (ActionSelectionManager.Instance == null || diary == null) return;

        Transform pageL = diary.transform.Find("Page_L");
        if (pageL == null) return;

        for (int i = 0; i < DiaryTimeSections.Length; i++)
        {
            Transform section = pageL.Find(DiaryTimeSections[i]);
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
