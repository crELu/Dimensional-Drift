using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TextMeshProUGUI weaponName;
    public bool IsEmpty { get; private set; } = true;

    public void SetWeapon(PlayerWeapon weapon)
    {
        if (weapon == null)
        {
            Clear();
            return;
        }

        IsEmpty = false;
        string weaponTypeName = weapon.WeaponType.ToString();
        weaponName.text = weaponTypeName;
        
        string iconPath = $"Sprites/Weapons/{weaponTypeName}";
        weaponIcon.sprite = Resources.Load<Sprite>(iconPath);
        weaponIcon.color = Color.white;
    }

    public void Clear()
    {
        IsEmpty = true;
        weaponIcon.sprite = null;
        weaponIcon.color = Color.clear;
        weaponName.text = "";
    }
} 