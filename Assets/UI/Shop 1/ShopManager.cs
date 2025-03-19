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
    [SerializeField] private TextMeshProUGUI infoTitle, infoDescription;
    [SerializeField] private ShopItem shopItemPrefab;

    [Header("Shop Items")]
    [SerializeField] private List<Item> availableItems = new();
    [SerializeField] private List<Transform> shopSlots = new();

    [Header("UI Navigation")]
    [SerializeField] private PlayerInput playerInput;
    [SerializeField] private Selectable firstShopSelectable;

    // private bool isShopActive = true;

    private void Start()
    {
        shopInvParentPanel.SetActive(false);
        playerInput.actions["Player/Shop"].performed += OnShopAction;
        playerInput.actions["UI/Shop"].performed += OnShopAction;

        InitializeShopItems();
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
        if (shopInvParentPanel.activeSelf || PlayerManager.waveTimer > 0)
            ToggleShop();
    }

    private void InitializeShopItems()
    {
        foreach (Transform child in shopSlots) Destroy(child.GetChild(0).gameObject);

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

    public void DisplayText(string text, string title)
    {
        infoTitle.text = title;
        infoDescription.text = text;
    }
    
    public void ClearText()
    {
        infoTitle.text = "";
        infoDescription.text = "";
    }
}