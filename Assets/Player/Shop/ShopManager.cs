using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

public class ShopManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private PlayerManager playerManager;

    [Header("UI Elements")]
    [SerializeField] private GameObject shopInvParentPanel;
    [SerializeField] private GameObject shopPage;
    [SerializeField] private GameObject inventoryPage;
    [SerializeField] private Button pageToggleButton;
    [SerializeField] private TextMeshProUGUI toggleButtonText;
    [SerializeField] private Transform shopItemsContainer;
    [SerializeField] private GameObject shopItemPrefab;
    [SerializeField] private TextMeshProUGUI waveCounter;

    [Header("Shop Items")]
    [SerializeField] private List<ShopItem> availableItems = new();

    [Header("UI Navigation")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Selectable firstShopSelectable;

    // private bool isShopActive = true;

    private void Start()
    {
        shopInvParentPanel.SetActive(false);
        pageToggleButton.onClick.AddListener(() => SwitchToPage(shopPage.activeSelf));
        playerInput.actions["Player/Shop"].performed += OnShopAction;
        playerInput.actions["UI/Shop"].performed += OnShopAction;

        InitializeShopItems();
        if (availableItems.Count > 0)
            EventSystem.current.SetSelectedGameObject(availableItems[0].gameObject);
    }

    private void OnDestroy()
    {
        playerInput.actions["Player/Shop"].performed -= OnShopAction;
        playerInput.actions["UI/Shop"].performed -= OnShopAction;
    }

    private void OnShopAction(InputAction.CallbackContext context)
    {
        if (shopInvParentPanel.activeSelf || PlayerManager.waveTimer > 0)
            ToggleShop();
    }

    private void InitializeShopItems()
    {
        foreach (Transform child in shopItemsContainer) Destroy(child.gameObject);

        foreach (var item in availableItems)
        {
            GameObject itemGO = Instantiate(shopItemPrefab, shopItemsContainer);
            itemGO.GetComponent<ShopItem>().Initialize(item.data, this);
        }

        if (shopItemsContainer.childCount > 0)
            firstShopSelectable = shopItemsContainer.GetChild(0).GetComponent<Selectable>();
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
            EventSystem.current.SetSelectedGameObject(firstShopSelectable.gameObject);
            SwitchToPage(true);
            playerInventory.UpdateUI();
        }
        else
        {
            Time.timeScale = 1f;
            playerInput.SwitchCurrentActionMap("Player");
        }
    }

    public void SwitchToPage(bool showShop)
    {
        shopPage.SetActive(showShop);
        inventoryPage.SetActive(!showShop);
        toggleButtonText.text = showShop ? "Shop" : "Inventory";
        if (!showShop) playerInventory.UpdateUI(); // Update inventory UI when switching
    }

    public bool PurchaseItem(ShopItemData item)
    {
        if (!item.augment.Requirements.All(req =>
            req.exclusion ? !playerInventory.HasAugment(req.augment, req.minTier)
                          : playerInventory.HasAugment(req.augment, req.minTier))) return false;
        if (!playerInventory.SpendIntel(item.cost)) return false;

        playerInventory.AddAugment(item.augment);
        return true;
    }
}