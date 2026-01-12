using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemShopSlot : MonoBehaviour
{
    [SerializeField] private Image ItemImage;
    [SerializeField] private TextMeshProUGUI NameLabel, LoreLabel, CostLabel, CountLabel;

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
        NameLabel.text = $"{foodData.ingredientName} <size=70%>({currnetCount}/{sellInfo.ItemAmount})</size>";
        CountLabel.text = currnetCount.ToString();
    }

    public void InitSlot(ItemShopSlotInfo sellItem)
    {
        foodData = ScriptableObject.CreateInstance<FoodData>();

        // 정보 가져오기
        sellInfo = sellItem;
        if (SearchDataUtil.GetFoodDataById(sellInfo.Id) is FoodData)
            foodData = SearchDataUtil.GetFoodDataById(sellInfo.Id);
        ingredientData = SearchDataUtil.GetIngredientDataById(sellInfo.Id);
        ItemImage.sprite = foodData.image;
    }
    public void BTN_PlusItem()
    {
        if (currnetCount >= sellInfo.ItemAmount) return;
        currnetCount++;
        UpdateSlot();
        ItemShopManager.Instance.AddProduct(foodData);
        ItemShopManager.Instance.AddPrice(ingredientData.defaultPrice);
        ItemShopManager.Instance.UpdateTotalPrice();
        ItemShopManager.Instance.CheckMoneyOver();
    }
    public void BTN_MinusItem()
    {
        if (currnetCount <= 0) return;
        currnetCount--;
        UpdateSlot();
        ItemShopManager.Instance.SubProduct(foodData);
        ItemShopManager.Instance.SubPrice(ingredientData.defaultPrice);
        ItemShopManager.Instance.UpdateTotalPrice();
        ItemShopManager.Instance.CheckMoneyOver();
    }
}
