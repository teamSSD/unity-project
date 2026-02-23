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
    private DiaryModel diaryModel;

    public void Initialize(IUnlockedFoodProvider foodProvider)
    {
    }

    public void Initialize(DiaryModel model)
    {
        this.diaryModel = model;

        if (diaryModel != null)
        {
            diaryModel.OnBentoFoodAdded += OnFoodAdded;
            diaryModel.OnBentoFoodRemoved += OnFoodRemoved;
            diaryModel.OnBentoLockedChanged += OnLockStateChanged;
        }

        Debug.Log("[BentoToggleList] DiaryModel injected and events subscribed");
    }

    private void OnDestroy()
    {
        if (diaryModel != null)
        {
            diaryModel.OnBentoFoodAdded -= OnFoodAdded;
            diaryModel.OnBentoFoodRemoved -= OnFoodRemoved;
            diaryModel.OnBentoLockedChanged -= OnLockStateChanged;
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
        if (diaryModel != null)
        {
            diaryModel.RemoveBentoFood(food);
        }
    }

    public void AddFood(FoodData food)
    {
        if (diaryModel != null)
        {
            diaryModel.AddBentoFood(food);
        }
    }

    private bool AddFood(Toggle toggle, FoodData food)
    {
        if (diaryModel != null)
        {
            return diaryModel.AddBentoFood(currentIndex, food);
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

        if (diaryModel != null)
        {
            var bento = diaryModel.GetBentoForDisplay(index);
            if (bento != null)
            {
                mainMenu = bento.MainMenu;
                sideMenus = bento.SideMenus;
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

        if (diaryModel == null || toggleRoot == null)
        {
            ignoreToggleEvent = false;
            return;
        }

        var bento = diaryModel.GetBentoForDisplay(bentoIndex);
        if (bento == null || (bento.MainMenu == null && bento.SideMenus.Count == 0))
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

            if (bento.MainMenu != null && slot.Id == bento.MainMenu.id)
            {
                toggle.isOn = false;
            }
            else if (bento.SideMenus.Any(f => f.id == slot.Id))
            {
                toggle.isOn = false;
            }
        }

        ignoreToggleEvent = false;
    }

    public bool HasAnySelection()
    {
        if (diaryModel != null)
        {
            return diaryModel.HasAnyBentoSelection();
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
        if (diaryModel != null)
        {
            var lines = new List<string>();
            for (int i = 0; i < diaryModel.GetBentoCount(); i++)
            {
                var bento = diaryModel.GetBentoForDisplay(i);
                string mainName = bento.MainMenu?.ingredientName ?? "None";
                string sideNames = string.Join(", ", bento.SideMenus.Select(f => f.ingredientName));
                lines.Add($"{bento.Name}: Main={mainName}, Sides=[{sideNames}]");
            }
            return string.Join("\n", lines);
        }

        return "No DiaryModel available";
    }
}
