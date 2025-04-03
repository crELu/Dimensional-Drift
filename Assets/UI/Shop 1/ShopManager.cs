using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem.Users;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerManager playerManager;

    [Header("UI Elements")]
    [SerializeField] private GameObject shopInvParentPanel;
    [SerializeField] private GameObject shopPage;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private TextMeshProUGUI infoTitle, infoDescription, infoCost;
    [SerializeField] private ShopItem shopItemPrefab;

    [Header("Shop Items")]
    [SerializeField] private List<Item> augmentPool;
    [SerializeField] private List<Item> shardPool;
    [SerializeField] private List<Item> availableItems = new();
    [SerializeField] private List<Transform> shopSlots = new();
    [SerializeField] public List<Sprite> rarities = new();
    
    [Header("UI Navigation")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Selectable firstShopSelectable;

    // private bool isShopActive = true;

    private void Start()
    {
        shopInvParentPanel.SetActive(false);
        playerInput.actions["Player/Shop"].performed += OnShopAction;
        playerInput.actions["UI/Shop"].performed += OnShopAction;
        RefreshShop();
        playerInput.SwitchCurrentActionMap("Player");
        // if (availableItems.Count > 0)
        //     EventSystem.current.SetSelectedGameObject(availableItems[0].gameObject);
    }

    private void OnDestroy()
    {
        playerInput.actions["Player/Shop"].performed -= OnShopAction;
        playerInput.actions["UI/Shop"].performed -= OnShopAction;
    }

    private void OnShopAction(InputAction.CallbackContext context)
    {
        ToggleShop();
    }

    private void InitializeShopItems()
    {
        foreach (Transform child in shopSlots) if (child.childCount != 0) Destroy(child.GetChild(0).gameObject);

        for (int i = 0; i < availableItems.Count; i++)
        {
            var item = availableItems[i];
            var cont = shopSlots[i];
            ShopItem itemGo = Instantiate(shopItemPrefab, cont);
            itemGo.Initialize(item, this);
        }
        
        // if (shopItemsContainer.childCount > 0)
        //     firstShopSelectable = shopItemsContainer.GetChild(0).GetComponent<Selectable>();
    }

    public void RefreshShop()
    {
        List<Item> validItems = new();
        foreach (var augment in augmentPool)
        {
            bool valid = false;
            int tier = 0;
            var (b, a) = playerManager.ValidAugment(((AugmentItem)augment).augment);
            if (b)
            {
                valid = true;
                tier = a;
            }
            foreach (var weapon in playerManager.weapons)
            {
                (b, a) = weapon.ValidAugment(((AugmentItem)augment).augment);
                if (b)
                {
                    valid = true;
                    tier = a;
                }
            }
            if (valid)
            {
                var clone = Instantiate(augment);
                clone.rarity = tier;
                clone.baseCost *= 1+tier;
                validItems.Add(clone);
            }
        }

        var randomAugments = Maths.GenerateRandomIndices(validItems.Count);
        var randomShards = Maths.GenerateRandomIndices(shardPool.Count);
        availableItems.Clear();
        for (int i = 0; i < 4; i++)
        {
            if (randomAugments.Count > i) availableItems.Add(validItems[randomAugments[i]]);
        }

        for (int i = 0; i < 10 - Mathf.Min(randomAugments.Count, 4); i++)
        {
            availableItems.Add(shardPool[randomShards[i]]);
        }
        InitializeShopItems();
    }

    public void ToggleShop()
    {
        bool newState = !shopInvParentPanel.activeSelf;
        shopInvParentPanel.SetActive(newState);

        if (newState)
        {
            Time.timeScale = 0f;
            playerInput.SwitchCurrentActionMap("UI");
            Cursor.lockState = CursorLockMode.Confined;
            //EventSystem.current.SetSelectedGameObject(firstShopSelectable.gameObject);
            SwitchToPage(true);
            playerInventory.UpdateUI();
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = playerManager.targetCursorMode;
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    public void SwitchToPage(bool showShop)
    {
        shopPage.SetActive(showShop);
        inventoryPage.SetActive(!showShop);
        if (!showShop) playerInventory.UpdateUI(); // Update inventory UI when switching
    }

    public void DisplayText(string text, string title, float cost)
    {
        infoTitle.text = title;
        infoDescription.text = text;
        infoCost.text = $"{cost:F0}";
    }
    
    public void ClearText()
    {
        infoTitle.text = "";
        infoDescription.text = "";
        infoCost.text = "";
    }
}