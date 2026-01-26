using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Bento
{
    public string mainId = "";
    public List<string> sideId;
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
                    Debug.LogError("RecipeBookManager instance not found in scene.");
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
            bentos[i].sideId = new List<string>();
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
                bentos[currentIndex].mainId = "";
            }
            else if (SearchDataUtil.GetFoodDataById(id).type == FoodType.SIDE)
            {
                bentos[currentIndex].sideId.Remove(id);
            }
            return;
        }

        if (SearchDataUtil.GetFoodDataById(id).type == FoodType.MAIN)
        {
            bentos[currentIndex].mainId = id;
        }
        else if (SearchDataUtil.GetFoodDataById(id).type == FoodType.SIDE)
        {
            bentos[currentIndex].sideId.Add(id);
        }
    }

    public void ClearToggle()
    {
        foreach (Toggle t in toggleRoot.GetComponentsInChildren<Toggle>())
        {
            t.isOn = false;
        }
    }

    public void SetPreset(int index)
    {
        ignoreToggleEvent = true;

        List<GameObject> menus = toggleRoot
            .GetComponentsInChildren<Transform>(true)
            .Where(t => t.CompareTag("DiaryMenu"))
            .Select(t => t.gameObject)
            .ToList();

        foreach (GameObject t in menus)
        {
            t.gameObject.GetComponent<Toggle>().isOn = false;

            if (t.gameObject.GetComponentInChildren<MenuSlot>().Id == bentos[index].mainId)
            {
                t.gameObject.GetComponent<Toggle>().isOn = true;
            }
            foreach (string to in bentos[index].sideId)
            {
                if (t.gameObject.GetComponentInChildren<MenuSlot>().Id == to)
                {
                    t.gameObject.GetComponent<Toggle>().isOn = true;
                }
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
            Debug.Log($"mainId = [{bentos[currentIndex].mainId}]");
        }
    }
    public void ChangeIndex_2(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 1;
            SetPreset(currentIndex);
            Debug.Log($"mainId = [{bentos[currentIndex].mainId}]");
        }
    }
    public void ChangeIndex_3(bool isOn)
    {
        if (isOn)
        {
            currentIndex = 2;
            SetPreset(currentIndex);
            Debug.Log($"mainId = [{bentos[currentIndex].mainId}]");
        }
    }
}
