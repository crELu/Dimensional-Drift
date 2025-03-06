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
    [SerializeField] private Transform shopItemsContainer;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private TextMeshProUGUI waveCounter;

    [SerializeField] private TextMeshProUGUI placeholderAugmentsText;
    [SerializeField] private TextMeshProUGUI placeholderStatsText;
    
    [Header("Shop Items")]
    [SerializeField] private List<ShopItemData> availableItems = new();

    [Header("UI Navigation")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private GameObject firstShopItem;  // PLACEHOLDER RRRHGH

    private void Start()
    {
        Debug.Log($"Shop panel reference: {shopInvParentPanel != null}");
        shopInvParentPanel.SetActive(false);
        InitializeShopItems();
        
        // Setup input callbacks for both action maps
        playerInput.actions["Player/Shop"].performed += OnShopAction;
        playerInput.actions["UI/Shop"].performed += OnShopAction;
        
        // Setup single toggle button
        pageToggleButton.onClick.AddListener(() => SwitchToPage(!shopPage.activeSelf));

        // Debug.Log(playerInput);
        
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

    private void OnDestroy()
    {
        // Clean up input callbacks
        if (playerInput != null)
        {
            playerInput.actions["Player/Shop"].performed -= OnShopAction;
            playerInput.actions["UI/Shop"].performed -= OnShopAction;
        }
    }

    private void OnShopAction(InputAction.CallbackContext context)
    {
        if (shopInvParentPanel.activeSelf || PlayerManager.waveTimer > 0)
        {
            ToggleShop();
        }
    }

    private void Update()
    {
        // if (shopActive)
        // {
        //     UpdateUI();
        // }
    }

    private void UpdateUI()
    {
        // Update intel text
        intelText.text = $"{playerInventory.Intel:F0}";
        
        // idk how to make wave stuff work atm will do it later
        // // Update wave counter text
        // if (PlayerManager.waveTimer > 0)
        // {
        //     waveCounter.text = $"Wave {Enemies.WaveManager.WaveSingleton.Wave} in {Mathf.Ceil(PlayerManager.waveTimer)}s";
        // }
        // else
        // {
        //     waveCounter.text = $"Wave {Enemies.WaveManager.WaveSingleton.Wave}";
        // }
        if (inventoryPage.activeSelf)
        {
            var augments = playerInventory.GetCharacterAugments();
            placeholderAugmentsText.text = "Equipped Augments:\n" + string.Join("\n", augments.Select(a => a.name));

            var stats = playerManager.stats;
            placeholderStatsText.text = $"Stats:\n" +
            $"Health: {stats.flatHealth}\n" +
            $"Shield: {stats.flatShield}\n" +
            $"Health Regen: {stats.percentHealth}\n" +
            $"Shield Regen: {stats.percentShield}\n" +
            $"Move Speed: {stats.moveSpeed}\n" +
            $"Contact Damage: {stats.contactDamage}\n" +
            $"Dash Cooldown: {stats.dashCd}\n" +
            $"Pickup Radius: {stats.pickupRadius}\n";

            UpdateWeaponSlots();
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

        // Update each slot
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
        inventoryPage.SetActive(!showShop);
        shopPage.SetActive(showShop);
        
        toggleButtonText.text = showShop ? "Inventory" : "Shop";
        UpdateUI();
    }

    public void ToggleShop()
    {
        bool newState = !shopInvParentPanel.activeSelf;
        Debug.Log($"ToggleShop called. Current state: {shopInvParentPanel.activeSelf}");
        shopInvParentPanel.SetActive(newState);
        Debug.Log($"Panel should now be: {newState}");
        
        if (newState)
        {
            Time.timeScale = 0f;
            playerInput.SwitchCurrentActionMap("UI");
            UpdateWeaponSlots();
            EventSystem.current.SetSelectedGameObject(firstShopItem);
            SwitchToPage(false);
        }
        else
        {
            Time.timeScale = 1f;
            playerInput.SwitchCurrentActionMap("Player");
        }
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