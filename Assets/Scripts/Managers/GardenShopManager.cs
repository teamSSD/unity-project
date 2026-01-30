using Codice.Client.BaseCommands;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        RefreshSlots();

        isItemShopActive = true;
    }

    public void CloseGardenShop()
    {
        shopRoot.SetActive(false);

        // Disable Toggle
        isItemShopActive = false;
    }

    public void BuyUpgrade()
    {
    }

    public void RefreshSlots()
    {
        //foreach (ItemShopSlot slot in currentSlots)
        //    slot.RefreshSlot();
    }

    public void CheckMoneyOver()
    {
    }
}
