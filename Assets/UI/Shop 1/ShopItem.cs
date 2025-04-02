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

// MonoBehaviour for UI
public class ShopItem : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [Header("UI Elements")]
    public Image itemIcon;
    public Image rarity;
    
    public Item data;
    private ShopManager shopManager;
    private PlayerInventory playerInventory;

    private void Start()
    {
    }

    public void OnSelect(BaseEventData eventData)
    {
        string rarityText = data.rarity == 0 ? "I" : data.rarity == 1 ? "II" : "III";
        shopManager.DisplayText($"{data.itemTitle} {rarityText}", data.Description);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        shopManager.ClearText();
    }

    public void Initialize(Item itemData, ShopManager manager)
    {
        data = itemData;
        shopManager = manager;
        playerInventory = PlayerManager.main.inventory;
        
        itemIcon.sprite = data.Icon;
        rarity.sprite = manager.rarities[data.rarity];
    }

    private void OnPurchaseClicked()
    {
        if (playerInventory.SpendIntel(data.baseCost))
        {
            data.DoAction();
            Destroy(gameObject);
        }
    }

} 