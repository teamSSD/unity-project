using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum FarmUpgradeType
{
    TileCount,
    TimeReduction,
    HarvestCount
}

public class GardenShopManager : MonoBehaviour
{
    private static GardenShopManager instance;
    public static GardenShopManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GardenShopManager>();
                if (instance == null)
                {
                    Debug.LogError("GardenShopManager instance not found in scene.");
                }
            }
            return instance;
        }
    }
    public static bool isItemShopActive { get; private set; } = false;

    [Header("상점 루트 오프젝트")]
    [SerializeField] public GameObject shopRoot;

    [Header("상점 오프젝트의 프리팹 인스턴스 트랜스폼")]
    [SerializeField] public Transform slotInstantiateTransform;

    [Header("상점 슬롯 프리팹")]
    [SerializeField] public GameObject shopSlotPrefab;

    private List<GardenShopSlot> currentSlots = new List<GardenShopSlot>();

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        isItemShopActive = false;
        shopRoot.SetActive(false);
    }
    public void OpenGardenShop()
    {
        if (isItemShopActive)
        {
            Debug.LogWarning("ItemShop is already open.");
            return;
        }

        shopRoot.SetActive(true);

        if (currentSlots.Count == 0)
        {
            CreateSlots();
        }

        RefreshSlots();

        isItemShopActive = true;
    }

    public void CloseGardenShop()
    {
        shopRoot.SetActive(false);

        // Disable Toggle
        isItemShopActive = false;
    }

    private void CreateSlots()
    {
        CreateSingleSlot(FarmUpgradeType.TileCount);
        CreateSingleSlot(FarmUpgradeType.TimeReduction);
        CreateSingleSlot(FarmUpgradeType.HarvestCount);
    }

    private void CreateSingleSlot(FarmUpgradeType type)
    {
        GameObject slotObj = Instantiate(shopSlotPrefab, slotInstantiateTransform);
        GardenShopSlot slotScript = slotObj.GetComponent<GardenShopSlot>();

        slotScript.SetupSlot(type);
        currentSlots.Add(slotScript);
    }

    public void BuyUpgrade(FarmUpgradeType type)
    {
        int currentGold = StatsSystem.Instance != null ? StatsSystem.Instance.GetMoney() : 0;
        bool isSuccess = false;

        switch (type)
        {
            case FarmUpgradeType.TileCount:
                isSuccess = FarmUpgradeManager.Instance.TryUpgradeTileCount(currentGold);
                break;
            case FarmUpgradeType.TimeReduction:
                isSuccess = FarmUpgradeManager.Instance.TryUpgradeTimeReduction(currentGold);
                break;
            case FarmUpgradeType.HarvestCount:
                isSuccess = FarmUpgradeManager.Instance.TryUpgradeHarvestCount(currentGold);
                break;
        }

        if (isSuccess)
        {
            RefreshSlots();
            CheckMoneyOver();
        }
    }

    public void RefreshSlots()
    {
        foreach (GardenShopSlot slot in currentSlots)
            slot.RefreshSlot();
    }

    public void CheckMoneyOver()
    {
    }
}
