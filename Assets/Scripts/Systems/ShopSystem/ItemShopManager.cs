using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
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
                instance = FindObjectOfType<ItemShopManager>();
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

    [Header("상점 루트 오브젝트")]
    [SerializeField] public GameObject shopRoot;

    [Header("상점 오브젝트 프리팹 인스턴스 트랜스폼")]
    [SerializeField] public Transform slotInstantiateTransform;

    [Header("상점 슬롯 프리팹")]
    [SerializeField] public GameObject shopSlotPrefab;

    [Header("총 가격")]
    [SerializeField] public TextMeshProUGUI totalPriceText;

    private List<ItemShopSlot> currentSlots = new List<ItemShopSlot>();
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
    public void OpenItemShop(ItemShopSlotInfo[] sellItems)
    {
        if (isItemShopActive)
        {
            Debug.LogWarning("ItemShop is already open.");
            return;
        }

        foreach (ItemShopSlotInfo sellItem in sellItems)
        {
            ItemShopSlot slot = Instantiate(shopSlotPrefab, Vector3.zero, Quaternion.identity, slotInstantiateTransform).GetComponent<ItemShopSlot>();
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
        totalPrice = 0;
        UpdateTotalPrice();
        shopRoot.SetActive(false);

        // 비활성화 토글
        isItemShopActive = false;
    }

    public void RefreshSlots()
    {
        foreach (ItemShopSlot slot in currentSlots)
            slot.RefreshSlot();
    }

    public int GetTotalPrice() { return totalPrice; }

    public void AddPrice(int price) { totalPrice += price; }

    public void UpdateTotalPrice() { totalPriceText.text = $"{totalPrice.ToString()}G"; }
}
