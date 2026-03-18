using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 도시락 메뉴 선택 UI를 관리하는 컨트롤러
/// - 3개의 도시락 세트(SelectionSet) 관리
/// - 메인/사이드 요리 선택 규칙(메인1, 사이드0-3) 적용
/// - RecipeDataManager와 연동하여 데이터 저장
/// </summary>
public class MenuSelectionUI : MonoBehaviour
{
    [System.Serializable]
    public class SelectionSetGroup
    {
        public GameObject root;
        public TMP_InputField nameInput;
        public Transform mainContainer;
        public Transform sideContainer;
        
        [HideInInspector]
        public List<MenuSelectionItem> mainItems = new List<MenuSelectionItem>();
        [HideInInspector]
        public List<MenuSelectionItem> sideItems = new List<MenuSelectionItem>();
    }

    [Header("UI Groups")]
    [SerializeField] private SelectionSetGroup[] selectionSets; // 0: 아침, 1: 점심, 2: 저녁
    [SerializeField] private Button nextButton;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button confirmButton;

    private int currentBentoIndex = 0;

    private void Start()
    {
        InitializeUI();
        UpdateView();
    }

    private void InitializeUI()
    {
        for (int i = 0; i < selectionSets.Length; i++)
        {
            var set = selectionSets[i];
            
            // 이름 입력 바인딩
            int index = i;
            set.nameInput.onEndEdit.AddListener((val) => OnNameChanged(index, val));
            
            // 기존 데이터 로드 (RecipeDataManager 연동 예정)
            LoadData(i);
        }

        if (nextButton != null) nextButton.onClick.AddListener(NextBento);
        if (prevButton != null) prevButton.onClick.AddListener(PrevBento);
        if (confirmButton != null) confirmButton.onClick.AddListener(ConfirmSelection);
    }

    private void UpdateView()
    {
        for (int i = 0; i < selectionSets.Length; i++)
        {
            selectionSets[i].root.SetActive(i == currentBentoIndex);
        }

        if (prevButton != null) prevButton.interactable = currentBentoIndex > 0;
        if (nextButton != null) nextButton.interactable = currentBentoIndex < selectionSets.Length - 1;
    }

    public void NextBento()
    {
        if (currentBentoIndex < selectionSets.Length - 1)
        {
            currentBentoIndex++;
            UpdateView();
        }
    }

    public void PrevBento()
    {
        if (currentBentoIndex > 0)
        {
            currentBentoIndex--;
            UpdateView();
        }
    }

    private void OnNameChanged(int bentoIndex, string newName)
    {
        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
        if (menu != null)
        {
            menu.Name = newName;
            Debug.Log($"[MenuSelectionUI] Bento {bentoIndex} name updated to: {newName}");
        }
    }

    private void LoadData(int bentoIndex)
    {
        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
        if (menu != null && selectionSets[bentoIndex].nameInput != null)
        {
            selectionSets[bentoIndex].nameInput.text = menu.Name;
        }
    }

    /// <summary>
    /// 메인 요리 선택 처리 (단일 선택)
    /// </summary>
    public void SelectMain(int bentoIndex, FoodData food)
    {
        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
        if (menu != null)
        {
            menu.SetMain(food);
            RefreshSelectionUI(bentoIndex);
        }
    }

    /// <summary>
    /// 사이드 요리 선택 처리 (최대 3개)
    /// </summary>
    public void ToggleSide(int bentoIndex, FoodData food)
    {
        var menu = RecipeDataManager.Instance.GetMenu(bentoIndex);
        if (menu != null)
        {
            if (menu.SideMenus.Contains(food))
            {
                menu.RemoveSide(food);
            }
            else if (menu.SideMenus.Count < 3)
            {
                menu.AddSide(food);
            }
            RefreshSelectionUI(bentoIndex);
        }
    }

    private void RefreshSelectionUI(int bentoIndex)
    {
        // TODO: UI 아이템들의 체크박스 상태 업데이트 로직 추가
        Debug.Log($"[MenuSelectionUI] Refreshed UI for Bento {bentoIndex}");
    }

    public void ConfirmSelection()
    {
        // 검증 로직 (최소 1개 도시락, 메인 메뉴 필수 등)
        if (!RecipeDataManager.Instance.HasAnySelection())
        {
            Debug.LogWarning("[MenuSelectionUI] No menus selected!");
            return;
        }

        // Scene_Mall의 확인 처리는 MallSceneController 등에서 처리하도록 이벤트나 콜백 연동 가능
        Debug.Log("[MenuSelectionUI] Selection confirmed and saved to RecipeDataManager");
    }
}
