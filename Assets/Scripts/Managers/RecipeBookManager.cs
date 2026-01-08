using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("레시피북 루트 오프젝트")]
    [SerializeField] public GameObject bookRoot;

    [Header("일지 페이지")]
    [SerializeField] public GameObject diary;

    [Header("메인메뉴 페이지")]
    [SerializeField] public GameObject mainMenu;

    [Header("사이드메뉴 페이지")]
    [SerializeField] public GameObject sideMenu;

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

    public void CloseRecipeBook()
    {
        bookRoot.SetActive(false);

        // 비활성화 토글
        isRecipeBookActive = false;
    }
}
