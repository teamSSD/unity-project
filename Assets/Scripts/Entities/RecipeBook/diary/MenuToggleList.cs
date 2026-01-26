using Codice.Client.Common;
using System.Collections.Generic;
using UnityEditor.Graphs;
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
    public Transform mainContent;
    public Transform sideContent;
    private List<string> mainMenuList;
    private List<string> sideMenuList;
    private void Awake()
    {
        mainMenuList = new List<string>();
        sideMenuList = new List<string>();

        foreach (Toggle t in toggleRoot.GetComponentsInChildren<Toggle>())
        {
            t.onValueChanged.AddListener((isOn) => OnToggleChanged(t, isOn));
        }
    }

    private void OnToggleChanged(Toggle toggle, bool isOn)
    {
        string id = toggle.gameObject.GetComponent<MenuSlot>().Id;

        Transform content = menuType == MenuType.Main ? mainContent : sideContent;

        if (isOn)
        {
            mainMenuList.Add(id);
            GameObject slot = Instantiate(menuSlot, content);
            slot.GetComponent<MenuSlot>().Id = id;
            slot.GetComponent<MenuSlot>().InitSlot();
            BentoToggleList.Instance.AddEvent(slot.GetComponent<Toggle>());
        }
        else
        {
            mainMenuList.Remove(id);
            foreach (MenuSlot item in content.gameObject.GetComponentsInChildren<MenuSlot>())
            {
                if (item.Id == id) Destroy(item.gameObject);
            }
        }
    }
    public List<string> GetMenuList()
    {
        if (menuType == MenuType.Main) return mainMenuList;
        if (menuType == MenuType.Side) return sideMenuList;
        return null;
    }
}