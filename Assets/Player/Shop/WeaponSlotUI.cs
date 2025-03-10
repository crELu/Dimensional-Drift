using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class WeaponSlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponName;
    [SerializeField] public AugmentType slotType;
    public bool IsEmpty { get; private set; } = true;

    [Header("Selection Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 1f);

    private PlayerWeapon currentWeapon;
    private PlayerInventory inventory;

    private void Awake()
    {
        inventory = FindFirstObjectByType<PlayerInventory>(); // Or inject via Inspector

        Selectable selectable = GetComponent<Selectable>();
        if (selectable != null)
        {
            // For Selectable, we'll need to use Unity's EventTrigger system instead of onClick
            EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();
            
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Select;
            entry.callback.AddListener((data) => { SelectSlot(); });
            
            eventTrigger.triggers.Add(entry);
        }
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (!IsEmpty)
        {
            weaponIcon.color = selectedColor;
            SelectSlot();
        }
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (!IsEmpty)
        {
            weaponIcon.color = normalColor;
        }
    }

    public void SetWeapon(PlayerWeapon weapon)
    {
        if (weapon == null)
        {
            Clear();
            return;
        }

        currentWeapon = weapon;
        IsEmpty = false;
        weaponName.text = weapon.WeaponType.ToString();
        
        string iconPath = $"shop_ui/placeholder_model";
        weaponIcon.sprite = Resources.Load<Sprite>(iconPath);
        weaponIcon.color = normalColor;
    }

    public PlayerWeapon GetWeapon() => currentWeapon;

    public void Clear()
    {
        IsEmpty = true;
        weaponIcon.sprite = null;
        weaponIcon.color = Color.clear;
        weaponName.text = "";
    }

    private void SelectSlot()
    {
        if (inventory.equippedWeapons.TryGetValue(slotType, out PlayerWeapon weapon))
        {
            inventory.SetSelectedWeapon(weapon);
        }
        else
        {
            inventory.SetSelectedWeapon(null);
        }
    }
}