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
    private int remainingStock;

    public void RefreshSlot()
    {
        currnetCount = 0;
        NameLabel.text = $"{foodData.ingredientName} <size=70%>(0/{remainingStock})</size>";
        LoreLabel.text = foodData.description;
        CostLabel.text = $"{ingredientData.defaultPrice}G";
        CountLabel.text = "0";
    }

    private void UpdateSlot()
    {
        NameLabel.text = $"{foodData.ingredientName} <size=70%>({currnetCount}/{remainingStock})</size>";
        CountLabel.text = currnetCount.ToString();
    }

    public void InitSlot(ItemShopSlotInfo sellItem, int alreadyBought = 0)
    {
        sellInfo = sellItem;
        foodData = sellItem.item;
        ingredientData = sellItem.item.ingredient;
        ItemImage.sprite = foodData.image;
        remainingStock = Mathf.Max(0, sellItem.stock - alreadyBought);
    }
    public void BTN_PlusItem()
    {
        if (currnetCount >= remainingStock) return;
        currnetCount++;
        UpdateSlot();
        UnifiedShopManager.Instance.AddProduct(foodData);
        UnifiedShopManager.Instance.AddPrice(ingredientData.defaultPrice);
        UnifiedShopManager.Instance.UpdateTotalPrice();
        UnifiedShopManager.Instance.CheckMoneyOver();
    }
    public void BTN_MinusItem()
    {
        if (currnetCount <= 0) return;
        currnetCount--;
        UpdateSlot();
        UnifiedShopManager.Instance.SubProduct(foodData);
        UnifiedShopManager.Instance.SubPrice(ingredientData.defaultPrice);
        UnifiedShopManager.Instance.UpdateTotalPrice();
        UnifiedShopManager.Instance.CheckMoneyOver();
    }
}
