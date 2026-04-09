using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 다이어리 Page_R에서 오늘 선택한 도시락 3개를 읽기 전용으로 표시.
/// Awake에서 계층구조 기반으로 자동 바인딩.
/// </summary>
public class DiaryMenuDisplay : MonoBehaviour
{
    private const int SlotCount = 3;
    private const int MaxSides = 3;

    private GameObject[] bentoSlots = new GameObject[SlotCount];
    private Image[] mainImages = new Image[SlotCount];
    private TextMeshProUGUI[] mainNames = new TextMeshProUGUI[SlotCount];
    private Image[,] sideImages = new Image[SlotCount, MaxSides];
    private TextMeshProUGUI[,] sideNames = new TextMeshProUGUI[SlotCount, MaxSides];
    private GameObject[,] sideSlots = new GameObject[SlotCount, MaxSides];

    private void Awake()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            var slot = transform.Find($"BentoSlot_{i}");
            if (slot == null) continue;
            bentoSlots[i] = slot.gameObject;

            var mainSlot = slot.Find("FoodRow/MainSlot");
            if (mainSlot != null)
            {
                mainImages[i] = mainSlot.Find("Image")?.GetComponent<Image>();
                mainNames[i] = mainSlot.Find("Name")?.GetComponent<TextMeshProUGUI>();

                // 메인 클릭 시 MenuCard 열기
                var btn = mainSlot.gameObject.GetComponent<Button>();
                if (btn == null) btn = mainSlot.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                int idx = i;
                btn.onClick.AddListener(() => OnMainClicked(idx));
            }

            var sideContainer = slot.Find("FoodRow/SideContainer");
            if (sideContainer != null)
            {
                for (int j = 0; j < MaxSides; j++)
                {
                    var side = sideContainer.Find($"SideSlot_{j}");
                    if (side == null) continue;
                    sideSlots[i, j] = side.gameObject;
                    sideImages[i, j] = side.Find("Image")?.GetComponent<Image>();
                    sideNames[i, j] = side.Find("Name")?.GetComponent<TextMeshProUGUI>();
                }
            }
        }
    }

    public void Refresh()
    {
        if (RecipeDataManager.Instance == null) return;

        for (int i = 0; i < SlotCount; i++)
        {
            if (bentoSlots[i] == null) continue;

            var menu = RecipeDataManager.Instance.GetMenu(i);
            if (menu == null || !menu.HasSelection())
            {
                bentoSlots[i].SetActive(false);
                continue;
            }

            bentoSlots[i].SetActive(true);
            SetFoodSlot(mainImages[i], mainNames[i], menu.MainMenu);

            for (int j = 0; j < MaxSides; j++)
            {
                if (j < menu.SideMenus.Count && menu.SideMenus[j] != null)
                {
                    if (sideSlots[i, j] != null) sideSlots[i, j].SetActive(true);
                    SetFoodSlot(sideImages[i, j], sideNames[i, j], menu.SideMenus[j]);
                }
                else
                {
                    ClearSlot(sideImages[i, j], sideNames[i, j]);
                }
            }
        }
    }

    private void SetFoodSlot(Image image, TextMeshProUGUI label, FoodData food)
    {
        if (image != null)
        {
            image.sprite = GetCookedImage(food);
            image.enabled = true;
        }
        if (label != null)
            label.text = food.ingredientName;
    }

    private Sprite GetCookedImage(FoodData food)
    {
        RecipeData recipe = SearchDataUtil.GetRecipeDataByFoodId(food.id);
        if (recipe != null && RecipeDataManager.Instance != null)
        {
            string toolId = RecipeDataManager.Instance.GetToolIdForMinigame(recipe.minigameId);
            if (!string.IsNullOrEmpty(toolId))
                return food.GetImageForTool(toolId);
        }
        return food.image;
    }

    private void ClearSlot(Image image, TextMeshProUGUI label)
    {
        if (image != null)
        {
            image.sprite = null;
            image.enabled = false;
        }
        if (label != null)
            label.text = "";
    }

    private void OnMainClicked(int index)
    {
        if (RecipeDataManager.Instance == null) return;
        var menu = RecipeDataManager.Instance.GetMenu(index);
        if (menu == null || !menu.HasSelection()) return;

        RecipeBookManager.Instance?.OpenMenuCardR(menu.MainMenu.id);
    }
}
