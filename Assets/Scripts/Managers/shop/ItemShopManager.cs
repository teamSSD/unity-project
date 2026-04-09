using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemShopManager : MonoBehaviour
{
    private static ItemShopManager instance;
    public static ItemShopManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<ItemShopManager>();
                if (instance == null)
                {
                    Debug.LogError("ItemShopManager instance not found in scene.");
                }
            }
            return instance;
        }
    }

    private static bool isItemShopActive = false;
    public static bool IsItemShopActive
    {
        get { return isItemShopActive; }
    }

    [Header("상점 루트 오프젝트")]
    [SerializeField] public GameObject shopRoot;

    [Header("상점 오프젝트의 프리팹 인스턴스 트랜스폼")]
    [SerializeField] public Transform slotInstantiateTransform;

    [Header("상점 슬롯 프리팹")]
    [SerializeField] public GameObject shopSlotPrefab;

    [Header("특별 상점 슬롯 프리팹")]
    [SerializeField] public GameObject specialShopSlotPrefab;

    [Header("총합 가격")]
    [SerializeField] public TextMeshProUGUI totalPriceText;

    [Header("확인 버튼")]
    [SerializeField] public Button selectButton;

    private List<ItemShopSlot> currentSlots = new List<ItemShopSlot>();
    private Dictionary<FoodData, int> purchaseList = new Dictionary<FoodData, int>();
    private int totalPrice = 0;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        totalPriceText.text = $"{totalPrice.ToString()}G";
        isItemShopActive = false;
        shopRoot.SetActive(false);
    }
    public void OpenItemShop(List<ItemShopSlotInfo> sellItems)
    {
        if (isItemShopActive)
        {
            Debug.LogWarning("ItemShop is already open.");
            return;
        }

        foreach (ItemShopSlotInfo sellItem in sellItems)
        {
            GameObject prefab = sellItem.ItemType == ProductType.Special ? specialShopSlotPrefab : shopSlotPrefab;
            ItemShopSlot slot = Instantiate(prefab, Vector3.zero, Quaternion.identity, slotInstantiateTransform).GetComponent<ItemShopSlot>();
            slot.InitSlot(sellItem);

            currentSlots.Add(slot);
        }
        shopRoot.SetActive(true);

        RefreshSlots();

        isItemShopActive = true;
    }

    public void CloseItemShop()
    {
        foreach (ItemShopSlot slot in currentSlots)
            Destroy(slot.gameObject);

        currentSlots.Clear();
        purchaseList.Clear();
        totalPrice = 0;
        UpdateTotalPrice();
        shopRoot.SetActive(false);

        // 비활성화 토글
        isItemShopActive = false;
    }

    public void BuyItem()
    {
        if (totalPrice <= 0) return;

        StatsSystem.Instance.SubMoney(totalPrice);
        foreach (var item in purchaseList)
            InventoryManager.Instance?.AddFood(item.Key, item.Value);

        // 저장은 PassDay()에서만 수행
        CloseItemShop();
    }

    public void RefreshSlots()
    {
        foreach (ItemShopSlot slot in currentSlots)
            slot.RefreshSlot();
    }

    public int GetTotalPrice() { return totalPrice; }

    public void AddPrice(int price) { totalPrice += price; }
    public void SubPrice(int price) { totalPrice -= price; }

    public void UpdateTotalPrice() { totalPriceText.text = $"{totalPrice.ToString()}G"; }

    public void CheckMoneyOver()
    {
        Color c;
        if (totalPrice > StatsSystem.Instance.GetMoney())
        {
            selectButton.interactable = false;
            selectButton.GetComponentInChildren<TextMeshProUGUI>().color = Color.gray5;
            ColorUtility.TryParseHtmlString("#FF8E94", out c);
            totalPriceText.color = c;   
        }
        else
        {
            selectButton.interactable = true;
            ColorUtility.TryParseHtmlString("#948A8A", out c);
            selectButton.GetComponentInChildren<TextMeshProUGUI>().color = c;
            totalPriceText.color = c;
        }
    }

    public void AddProduct(FoodData item)
    {
        if (purchaseList.ContainsKey(item)) purchaseList[item]++;
        else purchaseList.Add(item, 1);
    }
    public void SubProduct(FoodData item)
    {
        if (purchaseList.ContainsKey(item) && purchaseList[item] == 1) purchaseList.Remove(item);
        else purchaseList[item]--;
    }
}
