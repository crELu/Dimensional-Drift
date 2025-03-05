using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;

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
public class ShopItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI costText;
    public Button purchaseButton;

    private ShopItemData data;
    private ShopManager shopManager;

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