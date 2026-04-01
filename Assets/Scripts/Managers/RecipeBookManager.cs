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
            instance = FindObjectOfType<RecipeBookManager>();
            return instance != null;
        }
    }

    public static RecipeBookManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<RecipeBookManager>();
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
        if (Input.GetKeyDown(KeyCode.Tab) && !ClickStateUtil.globalLocked)
        {
            if (isRecipeBookActive)
                Close();
            else
                Open();
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
        OpenDiary();

        isRecipeBookActive = true;
    }
    public void CloseRecipeBook()
    {
        canvas.enabled = false;

        isRecipeBookActive = false;
    }
    public void OpenDiary()
    {
        diary.transform.SetAsLastSibling();
        CloseMenuCard();
    }
    public void OpenMainMenu()
    {
        mainMenu.transform.SetAsLastSibling();
        CloseMenuCard();
        if (UnlockedFoodManager.Instance != null)
            PopulateRecipePage(mainRecipeL, mainRecipeR,
                UnlockedFoodManager.Instance.GetUnlockedMainFoods());
    }
    public void OpenSideMenu()
    {
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
        if (MenuCard.Instance != null)
        {
            MenuCard.Instance.ClearMenuCard();
            MenuCard.Instance.InitSlot(id);
            return;
        }
        MenuCard card = Instantiate(menuCard, form).GetComponent<MenuCard>();
        card.InitSlot(id);
    }
    public void CloseMenuCard()
    {
        if (MenuCard.Instance != null)
        {
            MenuCard.Instance.CloseMenuCard();
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
