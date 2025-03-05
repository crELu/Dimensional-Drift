using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerManager playerManager;
    
    [Header("UI Elements")]
    [SerializeField] private GameObject shopInvParentPanel;
    [SerializeField] private TextMeshProUGUI intelText;
    [SerializeField] private GameObject shopPage;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private Button pageToggleButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;
    [SerializeField] private Transform weaponSlotsContainer;
    [SerializeField] private GameObject weaponSlotPrefab;
    [SerializeField] private Transform shopItemsContainer;
    [SerializeField] private GameObject shopItemPrefab;
    
    [Header("Shop Items")]
    [SerializeField] private List<ShopItemData> availableItems = new();

    [Header("UI Navigation")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject firstShopItem;
    private bool shopActive => shopInvParentPanel.activeSelf;

    private void Start()
    {
        shopInvParentPanel.SetActive(false);
        InitializeWeaponSlots();
        InitializeShopItems();
        
        // Setup single toggle button
        pageToggleButton.onClick.AddListener(() => SwitchToPage(!shopPage.activeSelf));
        
        // Start with shop inv page disabled
        // SwitchToPage(true);
        // UpdateUI();
    }

    private void Awake()
    {
        // Add some example shop items if the list is empty
        if (availableItems.Count == 0)
        {
            // placeholder add core augment
            var coreAugment = ScriptableObject.CreateInstance<CoreAugment>();
            availableItems.Add(new ShopItemData
            {
                itemTitle = "Basic Core Augment",
                description = "Enhances your weapon's core functionality",
                cost = 50f,
                icon = Resources.Load<Sprite>("Icons/CoreAugment"),
                augment = coreAugment
            });
            
            // maybe move list of all augments to a list of some sort
        }
    }

    private void Update()
    {
        if (InputSystem.actions.FindAction("Shop").WasPressedThisFrame() && PlayerManager.waveTimer > 0)
        {
            ToggleShop();
        }
        
        if (shopActive)
        {
            UpdateUI();
            
            if (InputSystem.actions.FindAction("Shop").WasPressedThisFrame())
            {
                ToggleShop();
            }
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

    private void SwitchToPage(bool showShop)
    {
        shopPage.SetActive(showShop);
        inventoryPage.SetActive(!showShop);
        toggleButtonText.text = showShop ? "Inventory" : "Shop";
        UpdateUI();
    }

    public void ToggleShop()
    {
        shopInvParentPanel.SetActive(!shopActive);
        
        if (shopActive)
        {
            Time.timeScale = 0f;
            playerInput.actions.FindActionMap("Player").Disable();
            playerInput.actions.FindActionMap("UI").Enable();
            UpdateWeaponSlots();
            EventSystem.current.SetSelectedGameObject(firstShopItem);
            SwitchToPage(false);  // Start with inventory page
        }
        else
        {
            Time.timeScale = 1f;
            playerInput.actions.FindActionMap("UI").Disable();
            playerInput.actions.FindActionMap("Player").Enable();
        }
        
        UpdateUI();
    }

    public bool PurchaseItem(ShopItemData item)
    {
        if (!item.augment.Requirements.All(req => 
            req.exclusion ? !playerInventory.HasAugment(req.augment, req.minTier) 
                         : playerInventory.HasAugment(req.augment, req.minTier))) return false;
        if (!playerInventory.SpendIntel(item.cost)) return false;

        playerInventory.AddAugment(item.augment);
        UpdateUI();
        return true;
    }
    
    public List<ShopItemData> GetAvailableItems(ShopItemType? filterType = null)
    {
        return filterType.HasValue 
            ? availableItems.Where(item => item.Type == filterType.Value).ToList() 
            : availableItems;
    }
} 