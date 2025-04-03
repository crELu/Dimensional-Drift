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
public class ShopItem : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image itemIcon;
    public Image rarity;
    public bool isPurchased = false;
    
    public Item data;
    private ShopManager shopManager;
    private PlayerInventory playerInventory;

    private void Start()
    {
    }

    public void OnSelect(BaseEventData eventData)
    {
        Select();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        Unselect();
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        Select();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Unselect();
    }

    public void Select()
    {
        if (isPurchased) return;
        string rarityText = data.rarity == 0 ? "I" : data.rarity == 1 ? "II" : "III";
        shopManager.DisplayText(data.Description, $"{data.itemTitle} {rarityText}", data.baseCost);
    }

    public void Unselect()
    {
        if (isPurchased) return;
        shopManager.ClearText();
    }

    public void Initialize(Item itemData, ShopManager manager)
    {
        data = Instantiate(itemData);
        shopManager = manager;
        playerInventory = PlayerManager.main.inventory;
        
        itemIcon.sprite = data.Icon;
        rarity.sprite = manager.rarities[data.rarity];
    }

    public void OnPurchaseClicked()
    {
        if (playerInventory.SpendIntel(data.baseCost))
        {
            data.DoAction();
            isPurchased = true;
            
            // Instead of disabling the parent, make it invisible and non-interactive
            CanvasGroup canvasGroup = transform.parent.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = transform.parent.gameObject.AddComponent<CanvasGroup>();
            
            canvasGroup.alpha = 0;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            // Update selection to the refresh button
            shopManager.UpdateUIItemPurchased();
            
            // Now destroy the item
            Destroy(gameObject);
        }
    }
} 