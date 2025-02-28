using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponName;

    public void SetWeapon(PlayerWeapon weapon)
    {
        if (weapon == null)
        {
            Clear();
            return;
        }

        // emptyState.SetActive(false);
        // filledState.SetActive(true);
        // weaponIcon.sprite = weapon.Icon;
        // weaponName.text = weapon.WeaponName;
        weaponIcon.sprite = Resources.Load<Sprite>("Sprites/Weapons/GunBasic");
        weaponName.text = "weapon";
    }

    public void Clear()
    {
        // emptyState.SetActive(true);
        // filledState.SetActive(false);
        weaponIcon.sprite = null;
        weaponName.text = "";
    }
} 