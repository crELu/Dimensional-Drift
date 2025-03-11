using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public enum ShopItemType
{
    CoreAugment,
    StatsAugment,
    SpecializedAugment,
    CharacterAugment
}

// Data class
[Serializable]
public class ShopItemData
{
    public string itemTitle;
    public string description;
    public float cost;
    public Sprite icon;
    public Augment augment;
    
    public ShopItemType Type
    {
        get
        {
            return augment switch
            {
                CoreAugment => ShopItemType.CoreAugment,
                StatsAugment => ShopItemType.StatsAugment,
                SpecializedAugment => ShopItemType.SpecializedAugment,
                CharacterAugment => ShopItemType.CharacterAugment,
                _ => throw new ArgumentException("Unknown augment type")
            };
        }
    }
}

// MonoBehaviour for UI
public class ShopItem : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI costText;
    public Button purchaseButton;
    public Augment augment;
    
    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 1f); // Light blue tint
    
    public ShopItemData data;
    private ShopManager shopManager;
    private PlayerInventory playerInventory;

    private void Start()
    {
        purchaseButton.interactable = false;  // Disabled by default
    }

    public void OnSelect(BaseEventData eventData)
    {
        itemIcon.color = selectedColor;
        purchaseButton.interactable = true;
    }

    public void OnDeselect(BaseEventData eventData)
    {
        itemIcon.color = normalColor;
        purchaseButton.interactable = false;
    }

    private void OnSubmit()
    {
        if (purchaseButton.interactable)
        {
            OnPurchaseClicked();
        }
    }

    public void Initialize(ShopItemData itemData, ShopManager manager)
    {
        data = itemData;
        shopManager = manager;
        
        itemIcon.sprite = data.icon;
        costText.text = $"{data.cost} Intel";
        
        purchaseButton.onClick.AddListener(() => OnPurchaseClicked());
    }

    private void OnPurchaseClicked()
    {
        if (shopManager.PurchaseItem(data))
        {
            // If it's a weapon-related augment, apply it immediately
            if (data.augment is CoreAugment coreAug)
            {
                var weapon = playerInventory.equippedWeapons[coreAug.coreType];
                if (weapon != null)
                {
                    // weapon.AddAugment(coreAug);
                }
            }
            purchaseButton.interactable = false;
        }
    }

    public bool CanPurchase(PlayerInventory inventory) => data.augment.Requirements.All(req => 
        req.exclusion ? !inventory.HasAugment(req.augment, req.minTier) 
                     : inventory.HasAugment(req.augment, req.minTier));

    public void Apply(PlayerInventory inventory, PlayerManager player)
    {
        inventory.AddAugment(data.augment);
    }
} 