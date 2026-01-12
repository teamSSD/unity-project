using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ResourcePaths;

public class RecipeBookManager : MonoBehaviour
{
    private static RecipeBookManager instance;
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

    [Header("Recipe Book Root")]
    [SerializeField] public GameObject bookRoot;

    [Header("Diary Page")]
    [SerializeField] public GameObject diary;

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
        isRecipeBookActive = false;

        bookRoot.SetActive(false);
    }
    public void OpenRecipeBook()
    {
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
    public void OpenDiary()
    {
        diary.transform.SetAsLastSibling();
    }
    public void OpenMainMenu()
    {
        mainMenu.transform.SetAsLastSibling();
    }
    public void OpenSideMenu()
    {
        sideMenu.transform.SetAsLastSibling();
    }

    public void OpenMenuCardL(string id)
    {
        MenuCard card = Instantiate(menuCard, cardInstantiateTransform_L).GetComponent<MenuCard>();
        card.InitSlot(id);
    }
    public void OpenMenuCardR(string id)
    {
        MenuCard card = Instantiate(menuCard, cardInstantiateTransform_R).GetComponent<MenuCard>();
        card.InitSlot(id);
    }

    public void CloseRecipeBook()
    {
        bookRoot.SetActive(false);

        isRecipeBookActive = false;
    }
}
