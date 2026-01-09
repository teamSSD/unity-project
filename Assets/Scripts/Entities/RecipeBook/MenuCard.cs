using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuCard : MonoBehaviour
{
    [SerializeField] private Image MenuImage;
    [SerializeField] private TextMeshProUGUI NameLabel;

    private SearchFoodUsecase FoodUsecase;

    private FoodData foodData = new FoodData();

    public void InitSlot(string id)
    {
        if (FoodUsecase == null)
            FoodUsecase = new TempSearchFoodUsecase();

        // Get Info
        if (FoodUsecase.Search(id) is FoodData)
            foodData = FoodUsecase.Search(id);
        NameLabel.text = foodData.ingredientName;
        MenuImage.sprite = Resources.Load<Sprite>(ResourcePaths.Art.FOOD + foodData.imageName);
    }

    public void CloseMenuCard()
    {
        Destroy(this.gameObject);
    }
}
