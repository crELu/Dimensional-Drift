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

[Serializable]
public class ShopItem : MonoBehaviour
{
    [Header("UI Elements")]
    public Image itemIcon;
    public TextMeshProUGUI itemName;
    public TextMeshProUGUI costText;
    public Button purchaseButton;

    [Header("Item Data")]
    public string itemTitle;
    public string description;
    public float cost;
    public Sprite icon;
    public Augment augment;
    
    private ShopManager shopManager;

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
    
    public bool CanPurchase(PlayerInventory inventory)
    {
        foreach (var req in augment.Requirements)
        {
            if (req.exclusion)
            {
                if (inventory.HasAugment(req.augment, req.minTier))
                    return false;
            }
            else
            {
                if (!inventory.HasAugment(req.augment, req.minTier))
                    return false;
            }
        }
        return true;
    }
    
    public void Apply(PlayerInventory inventory, PlayerManager player)
    {
        inventory.AddAugment(augment);
    }

    public void Initialize(ShopItem data, ShopManager manager)
    {
        itemTitle = data.itemTitle;
        description = data.description;
        cost = data.cost;
        icon = data.icon;
        augment = data.augment;
        shopManager = manager;
        
        itemIcon.sprite = icon;
        itemName.text = itemTitle;
        costText.text = $"{cost} Intel";
        
        purchaseButton.onClick.AddListener(() => OnPurchaseClicked());
    }

    private void OnPurchaseClicked()
    {
        if (shopManager.PurchaseItem(this))
        {
            purchaseButton.interactable = false;
        }
    }
} 