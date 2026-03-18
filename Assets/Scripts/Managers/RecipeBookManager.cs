using System.Collections.Generic;
using TMPro;
using UnityEditor;
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

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // DontDestroyOnLoad는 루트 GameObject에만 작동
        if (transform.parent != null)
        {
            Debug.LogWarning($"[RecipeBookManager] GameObject is not root, converting to independent Canvas");

            // Canvas 컴포넌트가 없으면 추가 (UI 렌더링을 위해 필수)
            if (GetComponent<Canvas>() == null)
            {
                var canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100; // 다른 UI 위에 표시

                gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
                gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                Debug.Log("[RecipeBookManager] Added Canvas components");
            }

            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);
        isRecipeBookActive = false;
        bookRoot.SetActive(false);
    }
    public void OpenRecipeBook(bool active)
    {
        bookRoot.transform.Find("Button_Close").GetComponentInChildren<Button>().gameObject.SetActive(active);

        if (isRecipeBookActive)
        {
            Debug.LogWarning("RecipeBook is already open.");
            return;
        }

        bookRoot.SetActive(true);
        foreach (MenuSlot slot in GetComponentsInChildren<MenuSlot>())
            slot.InitSlot();
        OpenDiary();

        isRecipeBookActive = true;
    }
    public void CloseRecipeBook()
    {
        bookRoot.SetActive(false);

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
    }
    public void OpenSideMenu()
    {
        sideMenu.transform.SetAsLastSibling();
        CloseMenuCard();
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

    /// <summary>
    /// 레시피북 열기 (읽기 전용)
    /// </summary>
    public void Open()
    {
        OpenRecipeBook(true); // Close button enabled
        Debug.Log("[RecipeBookManager] Opened (Read-only)");
    }

    /// <summary>
    /// 레시피북 닫기
    /// </summary>
    public void Close()
    {
        CloseRecipeBook();
        Debug.Log("[RecipeBookManager] Closed");
    }
}
