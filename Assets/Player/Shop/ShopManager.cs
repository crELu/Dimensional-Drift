using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerManager playerManager;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private TextMeshProUGUI intelText;
    [SerializeField] private Transform weaponSlotsContainer;
    [SerializeField] private GameObject weaponSlotPrefab;
    [SerializeField] private Transform shopItemsContainer;
    [SerializeField] private GameObject shopItemPrefab;
    
    [Header("Shop Items")]
    [SerializeField] private List<ShopItem> availableItems = new();

    private void Start()
    {
        shopPanel.SetActive(false);
        InitializeWeaponSlots();
        InitializeShopItems();
        UpdateUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleShop();
        }
        
        if (shopPanel.activeSelf)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        intelText.text = $"Intel: {playerInventory.Intel:F0}";
        UpdateWeaponSlots();
    }

    private void InitializeWeaponSlots()
    {
        // Clear existing slots if any
        foreach (Transform child in weaponSlotsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create new weapon slots
        for (int i = 0; i < playerInventory.maxWeaponSlots; i++)
        {
            Instantiate(weaponSlotPrefab, weaponSlotsContainer);
        }
    }

    private void InitializeShopItems()
    {
        // Clear existing items
        foreach (Transform child in shopItemsContainer)
        {
            Destroy(child.gameObject);
        }

        // Create shop item UI for each available item
        foreach (var item in availableItems)
        {
            GameObject itemGO = Instantiate(shopItemPrefab, shopItemsContainer);
            var itemUI = itemGO.GetComponent<ShopItem>();
            itemUI.Initialize(item, this);
        }
    }

    private void UpdateWeaponSlots()
    {
        var slots = weaponSlotsContainer.GetComponentsInChildren<WeaponSlotUI>();
        var equippedWeapons = playerInventory.equippedWeapons;

        for (int i = 0; i < slots.Length; i++)
        {
            if (i < equippedWeapons.Count)
            {
                slots[i].SetWeapon(equippedWeapons.ElementAt(i).Value);
            }
            else
            {
                slots[i].Clear();
            }
        }
    }

    public void ToggleShop()
    {
        shopPanel.SetActive(!shopPanel.activeSelf);
        if (shopPanel.activeSelf)
        {
            UpdateUI();
        }
    }

    public bool PurchaseItem(ShopItem item)
    {
        if (!item.CanPurchase(playerInventory)) return false;
        if (!playerInventory.SpendIntel(item.cost)) return false;

        item.Apply(playerInventory, playerManager);
        UpdateUI();
        return true;
    }
    
    public List<ShopItem> GetAvailableItems(ShopItemType? filterType = null)
    {
        return filterType.HasValue 
            ? availableItems.Where(item => item.Type == filterType.Value).ToList() 
            : availableItems;
    }
} 