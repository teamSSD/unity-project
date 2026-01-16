using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSlot : MonoBehaviour
{
    [SerializeField] private Image MenuImage;
    [SerializeField] private TextMeshProUGUI NameLabel;
    [Header("Menu ID")]
    [SerializeField] public string Id;

    private FoodData foodData;

    public void InitSlot()
    {
        foodData = ScriptableObject.CreateInstance<FoodData>();

        // Get Info
        if (SearchDataUtil.GetFoodDataById(Id) is FoodData)
            foodData = SearchDataUtil.GetFoodDataById(Id);
        NameLabel.text = foodData.ingredientName;
        MenuImage.sprite = foodData.image;
    }
    public void OpenMenuCardL()
    {
        RecipeBookManager.Instance.OpenMenuCardL(Id);
    }
    public void OpenMenuCardR()
    {
        RecipeBookManager.Instance.OpenMenuCardR(Id);
    }
}
