using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using JetBrains.Annotations;

public class ItemShopSlot : MonoBehaviour
{
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI NameLabel, LoreLabel, CostLabel, CountLabel;

    private SearchFoodUsecase FoodUsecase;
    private LoadInventoryUsecase InventoryUsecase;

    private FoodData foodData;
    private IngredientData ingredientData;
    private ItemShopSlotInfo sellInfo;
    private int currnetCount;
    public void RefreshSlot()
    {
        currnetCount = 0;
        NameLabel.text = $"{foodData.ingredientName} <size=70%>({currnetCount}/{sellInfo.ItemAmount})</size>";
        LoreLabel.text = foodData.description;
        CostLabel.text = $"{ingredientData.defaultPrice}G";
        CountLabel.text = currnetCount.ToString();
    }

    private void UpdateSlot()
    {
        NameLabel.text = $"{foodData.ingredientName} ({currnetCount}/{sellInfo.ItemAmount})";
        CountLabel.text = currnetCount.ToString();
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
    public void BTN_PlusItem()
    {
        if (currnetCount >= sellInfo.ItemAmount) return;
        currnetCount++;
        UpdateSlot();
        ItemShopManager.Instance.AddPrice(ingredientData.defaultPrice);
        ItemShopManager.Instance.UpdateTotalPrice();
    }
    public void BTN_MinusItem()
    {
        if (currnetCount <= 0) return;
        currnetCount--;
        UpdateSlot();
        ItemShopManager.Instance.SubPrice(ingredientData.defaultPrice);
        ItemShopManager.Instance.UpdateTotalPrice();
    }
}
