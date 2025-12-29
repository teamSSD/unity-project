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

    [Header("���� ��Ʈ ������Ʈ")]
    [SerializeField] public GameObject shopRoot;

    [Header("���� ������Ʈ ������ �ν��Ͻ� Ʈ������")]
    [SerializeField] public Transform slotInstantiateTransform;

    [Header("���� ���� ������")]
    [SerializeField] public GameObject shopSlotPrefab;

    [Header("�� ����")]
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

        // ��Ȱ��ȭ ���
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
