using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuSlot : MonoBehaviour
{
    [SerializeField] private Image MenuImage;
    [SerializeField] private TextMeshProUGUI NameLabel;
    [Header("Menu ID")]
    [SerializeField] public string Id;

    private SearchFoodUsecase FoodUsecase;

    private FoodData foodData = new FoodData();

    public void InitSlot()
    {
        if (FoodUsecase == null)
            FoodUsecase = new TempSearchFoodUsecase();

        // Get Info
        if (FoodUsecase.Search(Id) is FoodData)
            foodData = FoodUsecase.Search(Id);
        NameLabel.text = foodData.ingredientName;
        MenuImage.sprite = Resources.Load<Sprite>(ResourcePaths.Art.FOOD + foodData.imageName);
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
