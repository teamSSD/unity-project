using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum MenuType
{
    Main,
    Side,
}

public class MenuToggleList : MonoBehaviour
{
    public GameObject toggleRoot;
    public MenuType menuType;
    public GameObject menuSlot;
    public GameObject togglePrefab;
    public Transform mainContent;
    public Transform sideContent;
    private List<string> mainMenuList;
    private List<string> sideMenuList;
    private IUnlockedFoodProvider foodProvider;

    // Dependency Injection으로 전환
    private IBentoToggle bentoToggle;

    private void Awake()
    {
        mainMenuList = new List<string>();
        sideMenuList = new List<string>();

        // toggleRoot가 설정되지 않았거나 잘못 설정된 경우 자동으로 Content 찾기
        if (toggleRoot == null || toggleRoot.GetComponent<UnityEngine.UI.LayoutGroup>() == null)
        {
            // Scroll View / Viewport / Content 경로로 Content 찾기
            Transform scrollView = transform.Find("Scroll View");
            if (scrollView != null)
            {
                Transform viewport = scrollView.Find("Viewport");
                if (viewport != null)
                {
                    Transform content = viewport.Find("Content");
                    if (content != null)
                    {
                        toggleRoot = content.gameObject;
                        Debug.Log($"[MenuToggleList] Auto-found toggleRoot: {content.name}");
                    }
                }
            }

            if (toggleRoot == null)
            {
                Debug.LogWarning($"[MenuToggleList] Could not auto-find toggleRoot on {gameObject.name}. Please assign manually.");
            }
        }
    }

    private void OnEnable()
    {
        // RecipeDataManager에서 직접 초기화는 필요 없음
        // 외부에서 Initialize() 호출하여 사용
    }


    [ContextMenu("Manually Initialize (Editor)")]
    private void ManuallyInitialize()
    {
        Debug.LogWarning("[MenuToggleList] Manual initialization removed. Use Initialize(IUnlockedFoodProvider) instead.");
    }

    /// <summary>
    /// 의존성 주입으로 초기화
    /// </summary>
    public void Initialize(IUnlockedFoodProvider provider, IBentoToggle bentoToggle)
    {
        foodProvider = provider;
        this.bentoToggle = bentoToggle;

        if (toggleRoot == null || togglePrefab == null)
        {
            Debug.LogWarning($"[MenuToggleList] toggleRoot or togglePrefab is null. Skipping initialization.");
            return;
        }

        ClearExistingToggles();
        GenerateTogglesFromUnlockedFoods();

        Debug.Log($"[MenuToggleList] Initialized with dependency injection.");
    }


    /// <summary>
    /// Legacy Initialize method for backward compatibility
    /// </summary>
    public void Initialize(IUnlockedFoodProvider provider)
    {
        foodProvider = provider;

        if (toggleRoot == null || togglePrefab == null)
        {
            Debug.LogWarning($"[MenuToggleList] toggleRoot or togglePrefab is null. Skipping initialization.");
            return;
        }

        ClearExistingToggles();
        GenerateTogglesFromUnlockedFoods();
    }

    private void ClearExistingToggles()
    {
        foreach (Transform child in toggleRoot.transform)
        {
            Destroy(child.gameObject);
        }
    }

    private void GenerateTogglesFromUnlockedFoods()
    {
        List<FoodData> foods = menuType == MenuType.Main
            ? foodProvider.GetUnlockedMainFoods()
            : foodProvider.GetUnlockedSideFoods();

        foreach (var food in foods)
        {
            GameObject toggleObj = Instantiate(togglePrefab, toggleRoot.transform);

            MenuSlot menuSlotComponent = toggleObj.GetComponent<MenuSlot>();
            if (menuSlotComponent != null)
            {
                menuSlotComponent.Id = food.id;
                menuSlotComponent.InitSlot();
            }

            Toggle toggle = toggleObj.GetComponent<Toggle>();
            if (toggle != null)
            {
                toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(toggle, isOn));
            }
        }

        // UI 레이아웃 강제 갱신
        if (toggleRoot != null)
        {
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(toggleRoot.GetComponent<RectTransform>());
        }

        Debug.Log($"[MenuToggleList] Generated {foods.Count} {menuType} toggles.");
    }

    /// <summary>
    /// Toggle 리스너 등록 (테스트에서도 사용 가능하도록 public으로 분리)
    /// </summary>
    public void RegisterToggleListeners()
    {
        if (toggleRoot == null)
        {
            return;
        }

        foreach (Toggle t in toggleRoot.GetComponentsInChildren<Toggle>())
        {
            // MenuSlot이 있는 Toggle만 리스너 등록
            if (t.gameObject.GetComponent<MenuSlot>() != null)
            {
                t.onValueChanged.AddListener((isOn) => OnToggleChanged(t, isOn));
            }
            else
            {
                Debug.LogWarning($"[MenuToggleList] Skipping {t.gameObject.name} - no MenuSlot component found.");
            }
        }
    }

    private void OnToggleChanged(Toggle toggle, bool isOn)
    {
        MenuSlot menuSlotComponent = toggle.gameObject.GetComponent<MenuSlot>();
        if (menuSlotComponent == null)
        {
            Debug.LogError($"[MenuToggleList] MenuSlot component not found on {toggle.gameObject.name}!");
            return;
        }

        string id = menuSlotComponent.Id;
        Transform content = menuType == MenuType.Main ? mainContent : sideContent;

        if (isOn)
        {
            // menuType에 따라 적절한 리스트에 추가
            if (menuType == MenuType.Main)
            {
                mainMenuList.Add(id);
            }
            else if (menuType == MenuType.Side)
            {
                sideMenuList.Add(id);
            }

            GameObject slot = Instantiate(menuSlot, content);
            var slotComponent = slot.GetComponent<MenuSlot>();
            if (slotComponent != null)
            {
                slotComponent.Id = id;

                // InitSlot은 Resources가 필요하므로 안전하게 호출
                try
                {
                    slotComponent.InitSlot();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[MenuToggleList] Failed to initialize slot {id}: {e.Message}");
                }
            }

            // Use injected IBentoToggle instead of singleton
            if (bentoToggle != null)
            {
                // BentoToggleList는 IBentoToggle 인터페이스만 노출하므로
                // AddEvent는 BentoToggleList의 public 메서드로 직접 호출 필요
                // 임시로 타입 캐스팅 (Phase 3에서 이벤트 기반으로 개선 예정)
                if (bentoToggle is BentoToggleList bentoList)
                {
                    var slotToggle = slot.GetComponent<Toggle>();
                    if (slotToggle != null)
                    {
                        bentoList.AddEvent(slotToggle);
                    }
                }
            }
        }
        else
        {
            // menuType에 따라 적절한 리스트에서 제거
            if (menuType == MenuType.Main)
            {
                mainMenuList.Remove(id);
            }
            else if (menuType == MenuType.Side)
            {
                sideMenuList.Remove(id);
            }
            foreach (MenuSlot item in content.gameObject.GetComponentsInChildren<MenuSlot>())
            {
                if (item.Id == id) 
                {
#if UNITY_EDITOR
                    if (UnityEditor.Selection.activeGameObject != null && 
                        UnityEditor.Selection.activeGameObject.transform.IsChildOf(item.transform))
                    {
                        UnityEditor.Selection.activeGameObject = null;
                    }
#endif
                    Destroy(item.gameObject);
                }
            }
        }
    }
    [ContextMenu("Print Menu List")]
    public void PrintMenuList()
    {
        GetMenuList();
    }

    public List<string> GetMenuList()
    {
        if (menuType == MenuType.Main)
        {
            Debug.Log($"[MenuToggleList] GetMenuList (Main) Count: {mainMenuList.Count}, Items: {string.Join(", ", mainMenuList)}");
            return mainMenuList;
        }
        if (menuType == MenuType.Side)
        {
            Debug.Log($"[MenuToggleList] GetMenuList (Side) Count: {sideMenuList.Count}, Items: {string.Join(", ", sideMenuList)}");
            return sideMenuList;
        }
        return null;
    }

}