using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BentoToggleList : MonoBehaviour, IBentoValidator, IBentoToggle
{
    public GameObject toggleRoot;
    private int currentIndex = 0;
    private bool ignoreToggleEvent = false;
    private bool isReadOnlyMode = false;

    public void Initialize(IUnlockedFoodProvider foodProvider)
    {
    }

    public void Initialize()
    {
        // RecipeDataManager는 Singleton이므로 별도 Initialize 불필요
        Debug.Log("[BentoToggleList] Initialized");
    }

    private void Update()
    {
        // 읽기 전용일 때 상호작용 비활성화 (RecipeBook은 항상 읽기 전용)
        bool shouldDisable = RecipeBookManager.IsReadOnly;

        if (shouldDisable != isReadOnlyMode)
        {
            isReadOnlyMode = shouldDisable;
            SetInteractivity(!isReadOnlyMode);
        }
    }

    public void AddEvent(Toggle toggle)
    {
        if (toggle == null) return;
        toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(toggle, isOn));
    }

    private void OnToggleChanged(Toggle toggle, bool isOn)
    {
        if (ignoreToggleEvent || toggle == null) return;

        var food = GetFoodFromToggle(toggle);
        if (food == null) return;

        if (!isOn)
        {
            RemoveFood(food);
        }
        else
        {
            bool success = AddFood(toggle, food);
            if (!success)
            {
                ignoreToggleEvent = true;
                toggle.isOn = false;
                ignoreToggleEvent = false;
            }
        }
    }

    private FoodData GetFoodFromToggle(Toggle toggle)
    {
        MenuSlot slot = toggle.gameObject.GetComponentInChildren<MenuSlot>();
        if (slot == null) return null;

        return SearchDataUtil.GetFoodDataById(slot.Id);
    }

    public void RemoveFood(FoodData food)
    {
        if (RecipeDataManager.Instance == null) return;

        var menu = RecipeDataManager.Instance.GetMenu(currentIndex);
        if (menu != null)
        {
            if (menu.MainMenu == food)
            {
                menu.MainMenu = null;
            }
            else
            {
                menu.RemoveSide(food);
            }
        }
    }

    public void AddFood(FoodData food)
    {
        if (RecipeDataManager.Instance == null) return;

        var menu = RecipeDataManager.Instance.GetMenu(currentIndex);
        if (menu != null && food != null)
        {
            if (food.type == FoodType.MAIN)
            {
                menu.SetMain(food);
            }
            else
            {
                menu.AddSide(food);
            }
        }
    }

    private bool AddFood(Toggle toggle, FoodData food)
    {
        if (RecipeDataManager.Instance == null) return false;

        var menu = RecipeDataManager.Instance.GetMenu(currentIndex);
        if (menu == null || food == null) return false;

        if (food.type == FoodType.MAIN)
        {
            menu.SetMain(food);
            return true;
        }
        else if (food.type == FoodType.SIDE)
        {
            if (menu.SideMenus.Count < 3)
            {
                menu.AddSide(food);
                return true;
            }
            return false; // 사이드 메뉴 최대 3개
        }
        return false;
    }

    private Toggle FindToggleById(string targetId)
    {
        if (toggleRoot == null) return null;

        List<GameObject> menus = toggleRoot
            .GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("DiaryMenu"))
            .Select(t => t.gameObject)
            .ToList();

        foreach (var go in menus)
        {
            if (go == null) continue;
            MenuSlot s = go.GetComponent<MenuSlot>();
            if (s != null && s.Id == targetId)
            {
                Toggle t = go.GetComponent<Toggle>();
                if (t != null) return t;
            }
        }
        return null;
    }

    public void SetPreset(int index)
    {
        ignoreToggleEvent = true;
        currentIndex = index;

        FoodData mainMenu = null;
        List<FoodData> sideMenus = new List<FoodData>();

        if (RecipeDataManager.Instance != null)
        {
            var menu = RecipeDataManager.Instance.GetMenu(index);
            if (menu != null)
            {
                mainMenu = menu.MainMenu;
                sideMenus = menu.SideMenus;
            }
        }

        if (mainMenu == null && sideMenus.Count == 0)
        {
            ignoreToggleEvent = false;
            return;
        }

        List<GameObject> menus = toggleRoot
            .GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("DiaryMenu"))
            .Select(t => t.gameObject)
            .ToList();

        foreach (GameObject t in menus)
        {
            if (t == null) continue;
            Toggle toggle = t.GetComponent<Toggle>();
            MenuSlot slot = t.GetComponent<MenuSlot>();

            if (toggle == null || slot == null) continue;

            toggle.isOn = false;

            if (mainMenu != null && slot.Id == mainMenu.id)
            {
                toggle.isOn = true;
            }
            else if (sideMenus.Any(f => f.id == slot.Id))
            {
                toggle.isOn = true;
            }
        }

        ignoreToggleEvent = false;
    }

    public void ChangeIndex_1(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 0;
            SetPreset(currentIndex);
        }
        else
        {
            ClearBentoMenuToggles(0);
        }
    }

    public void ChangeIndex_2(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 1;
            SetPreset(currentIndex);
        }
        else
        {
            ClearBentoMenuToggles(1);
        }
    }

    public void ChangeIndex_3(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 2;
            SetPreset(currentIndex);
        }
        else
        {
            ClearBentoMenuToggles(2);
        }
    }

    private void ClearBentoMenuToggles(int bentoIndex)
    {
        ignoreToggleEvent = true;

        if (RecipeDataManager.Instance == null || toggleRoot == null)
        {
            ignoreToggleEvent = false;
            return;
        }

        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
        if (menu == null || (menu.MainMenu == null && menu.SideMenus.Count == 0))
        {
            ignoreToggleEvent = false;
            return;
        }

        List<GameObject> menus = toggleRoot
            .GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("DiaryMenu"))
            .Select(t => t.gameObject)
            .ToList();

        foreach (GameObject t in menus)
        {
            if (t == null) continue;
            Toggle toggle = t.GetComponent<Toggle>();
            MenuSlot slot = t.GetComponent<MenuSlot>();

            if (toggle == null || slot == null) continue;

            if (menu.MainMenu != null && slot.Id == menu.MainMenu.id)
            {
                toggle.isOn = false;
            }
            else if (menu.SideMenus.Any(f => f.id == slot.Id))
            {
                toggle.isOn = false;
            }
        }

        ignoreToggleEvent = false;
    }

    public bool HasAnySelection()
    {
        if (RecipeDataManager.Instance != null)
        {
            return RecipeDataManager.Instance.HasAnySelection();
        }
        return false;
    }

    public string GetSummary()
    {
        return ToString();
    }

    public void SetInteractivity(bool clickable)
    {
        CanvasGroup group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.interactable = clickable;
            group.blocksRaycasts = clickable;
        }
    }

    private void OnFoodAdded(int bentoIndex, FoodData food)
    {
        Debug.Log($"[BentoToggleList] OnFoodAdded: {food.ingredientName} to Bento {bentoIndex}");
    }

    private void OnFoodRemoved(int bentoIndex, FoodData food)
    {
        Debug.Log($"[BentoToggleList] OnFoodRemoved: {food.ingredientName} from Bento {bentoIndex}");
    }

    private void OnLockStateChanged(bool isLocked)
    {
        Debug.Log($"[BentoToggleList] OnLockStateChanged: {isLocked}");
        SetInteractivity(!isLocked);
    }

    public override string ToString()
    {
        if (RecipeDataManager.Instance != null)
        {
            var lines = new List<string>();
            for (int i = 0; i < 3; i++) // 3 menus: breakfast, lunch, dinner
            {
                var menu = RecipeDataManager.Instance.GetMenu(i);
                if (menu != null)
                {
                    string mainName = menu.MainMenu?.ingredientName ?? "None";
                    string sideNames = string.Join(", ", menu.SideMenus.Select(f => f.ingredientName));
                    lines.Add($"{menu.Name}: Main={mainName}, Sides=[{sideNames}]");
                }
            }
            return string.Join("\n", lines);
        }

        return "No RecipeDataManager available";
    }
}
