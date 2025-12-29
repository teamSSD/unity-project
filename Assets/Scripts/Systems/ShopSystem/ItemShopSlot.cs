using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemShopSlot : MonoBehaviour
{
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI NameLabel, LoreLabel, CostLabel;
    [SerializeField] private Button BuyButton;

    private SearchFoodUsecase FoodUsecase;
    private LoadInventoryUsecase InventoryUsecase;

    private FoodData foodData;
    private IngredientData ingredientData;
    private ItemShopSlotInfo sellInfo;
    private int currnetCount;
    public void RefreshSlot()
    {
        NameLabel.text = foodData.ingredientName;
        LoreLabel.text = foodData.description;
        CostLabel.text = $"{ingredientData.defaultPrice}G";
        currnetCount = 0;
        BuyButton.GetComponentInChildren<TextMeshProUGUI>().text = currnetCount.ToString();
    }

    public void InitSlot(ItemShopSlotInfo sellItem)
    {
        if (FoodUsecase == null)
            FoodUsecase = new TempSearchFoodUsecase();

        if (InventoryUsecase == null)
            InventoryUsecase = new TempLoadInventoryUsecase(FoodUsecase);
        // 정보 가져오기
        sellInfo = sellItem;
        foodData = FoodUsecase.Search(sellInfo.Id);
        ingredientData = InventoryUsecase.Search(sellInfo.Id).Item1;
        ItemImage.sprite = Resources.Load<Sprite>(ResourcePaths.Art.FOOD + foodData.imageName);
    }
    public void BTN_BuyItem()
    {
        BuyButton.GetComponentInChildren<TextMeshProUGUI>().text = (++currnetCount).ToString();
        ItemShopManager.Instance.AddPrice(ingredientData.defaultPrice);
        ItemShopManager.Instance.UpdateTotalPrice();
    }
}
