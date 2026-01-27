using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Bento
{
    private GameObject mainMenu;
    private List<GameObject> sideMenu;

    public string GetMainId() { return mainMenu.GetComponent<MenuSlot>().Id; }
    public List<string> GetSideIds()
    { 
        return sideMenu
            .Where(go => go != null)
            .Select(go => go.GetComponent<MenuSlot>())
            .Where(slot => slot != null && !string.IsNullOrEmpty(slot.Id))
            .Select(slot => slot.Id)
            .ToList();
    }

    public void SetMainMenu(GameObject m) { mainMenu = m; }
    public GameObject GetMainMenu() { return mainMenu; }
    public void SetSideMenu(List<GameObject> s) { sideMenu = s; }
    public void AddSideMenu(GameObject s) { sideMenu.Add(s); }
    public void RemoveSideMenu(GameObject s) { sideMenu.Remove(s); }
    public List<GameObject> GetSideMenu() { return sideMenu; }
}

public class BentoToggleList : MonoBehaviour
{
    private static BentoToggleList instance;
    public static BentoToggleList Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<BentoToggleList>();
                if (instance == null)
                {
                    Debug.LogError("BentoToggleList instance not found in scene.");
                }
            }
            return instance;
        }
    }

    public GameObject toggleRoot;
    private Bento[] bentos = new Bento[3];
    private int currentIndex = 0;
    private bool ignoreToggleEvent = false;
    private void Awake()
    {
        for (int i = 0; i < bentos.Length; i++)
        {
            bentos[i] = new Bento();
            bentos[i].SetSideMenu(new List<GameObject>());
        }
    }

    public void AddEvent(Toggle toggle)
    {
        toggle.onValueChanged.AddListener((isOn) => OnToggleChanged(toggle, isOn));
    }

    private void OnToggleChanged(Toggle toggle, bool isOn)
    {
        if (ignoreToggleEvent) return;

        string id = toggle.gameObject.GetComponentInChildren<MenuSlot>().Id;

        if (!isOn)
        {
            if (SearchDataUtil.GetFoodDataById(id).type == FoodType.MAIN)
            {
                bentos[currentIndex].SetMainMenu(null);
            }
            else if (SearchDataUtil.GetFoodDataById(id).type == FoodType.SIDE)
            {
                bentos[currentIndex].RemoveSideMenu(toggle.gameObject);
            }
            return;
        }

        if (SearchDataUtil.GetFoodDataById(id).type == FoodType.MAIN)
        {
            if (bentos[currentIndex].GetMainMenu() != null)
                bentos[currentIndex].GetMainMenu().GetComponent<Toggle>().isOn = false;
            bentos[currentIndex].SetMainMenu(toggle.gameObject);
        }
        else if (SearchDataUtil.GetFoodDataById(id).type == FoodType.SIDE)
        {
            bentos[currentIndex].AddSideMenu(toggle.gameObject);
        }
    }

    public Bento[] GetBentoList() { return bentos; }

    public void SetPreset(int index)
    {
        ignoreToggleEvent = true;

        if (bentos[index] == null)
        {
            ignoreToggleEvent = false;
            return;
        }

        string mainMenuId = bentos[index].GetMainMenu()?.GetComponent<MenuSlot>()?.Id;

        List<GameObject> menus = toggleRoot
            .GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("DiaryMenu"))
            .Select(t => t.gameObject)
            .ToList();

        foreach (GameObject t in menus)
        {
            Toggle toggle = t.GetComponent<Toggle>();
            MenuSlot slot = t.GetComponent<MenuSlot>();

            if (toggle == null || slot == null)
                continue;

            toggle.isOn = false;

            if (!string.IsNullOrEmpty(mainMenuId) && slot.Id == mainMenuId)
            {
                toggle.isOn = true;
            }
            else if (bentos[index].GetSideIds().Contains(slot.Id))
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
    }
    public void ChangeIndex_2(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 1;
            SetPreset(currentIndex);
        }
    }
    public void ChangeIndex_3(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 2;
            SetPreset(currentIndex);
        }
    }
}
