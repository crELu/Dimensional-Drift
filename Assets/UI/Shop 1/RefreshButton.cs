using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class RefreshButton : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, IPointerExitHandler
{
    public float cost;
    public ShopManager shopManager;
    public PlayerInventory playerInventory;
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

    private void Select()
    {
        shopManager.DisplayText("Refreshes the augment selection.", $"Refresh Shop", cost);
    }

    private void Unselect()
    {
        shopManager.ClearText();
    }

    public void OnPurchaseClicked()
    {
        if (playerInventory.SpendIntel(cost))
        {
            shopManager.RefreshShop();
            Select();
        }
    }
}