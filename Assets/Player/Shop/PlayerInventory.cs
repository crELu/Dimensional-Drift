using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private int maxWeaponSlots = 4;
    [SerializeField] private Transform weaponSlotsContainer;
    [SerializeField] private TextMeshProUGUI intelText;
    [SerializeField] private TextMeshProUGUI augmentsText;
    [SerializeField] private TextMeshProUGUI statsText;

    public Dictionary<AugmentType, PlayerWeapon> equippedWeapons = new();
    public List<Augment> ownedAugments = new();
    public PlayerWeapon selectedWeapon;
    public float Intel { get; private set; }

    private void Start()
    {
        Intel = 100;
        InitializeDefaultWeapons();
        UpdateUI();
    }

    private void InitializeDefaultWeapons()
    {
        // Placeholder for default weapon initialization
    }

    public void AddAugment(Augment augment)
    {
        ownedAugments.Add(augment);
        UpdateUI();
    }

    public bool HasAugment(Augment augment, int minTier)
    {
        return ownedAugments.Any(a => a.GetType() == augment.GetType() && a.Stacks >= minTier);
    }

    public List<Augment> GetCompatibleAugments(PlayerWeapon weapon) 
    { 
        return ownedAugments.Where(augment => 
        {
            if (augment is CoreAugment coreAug)
                return coreAug.coreType == weapon.WeaponType;
            if (augment is SpecializedAugment specAug)
                return specAug.coreType == weapon.WeaponType;
            if (augment is StatsAugment statsAug)
                return statsAug.type == StatsAugment.FitType.Weapon;
            return false;
        }).ToList();
    }
    public List<Augment> GetCharacterAugments() 
    {
        return ownedAugments.Where(augment => 
        {
            if (augment is CharacterAugment)
                return true;
            if (augment is StatsAugment statsAug)
                return statsAug.type == StatsAugment.FitType.Character;
            return false;
        }).ToList();
    }

    public void AddIntel(float amount) { Intel += amount; UpdateUI(); }
    public bool SpendIntel(float amount)
    {
        if (Intel >= amount)
        {
            Intel -= amount;
            UpdateUI();
            return true;
        }
        return false;
    }

    public void UpdateUI()
    {
        intelText.text = $"{Intel:F0}";
        UpdateWeaponSlots();
        UpdateAugmentsDisplay();
    }

    private void UpdateWeaponSlots()
    {
        var slots = weaponSlotsContainer.GetComponentsInChildren<WeaponSlotUI>();
        foreach (var slot in slots)
        {
            if (equippedWeapons.TryGetValue(slot.slotType, out PlayerWeapon weapon))
                slot.SetWeapon(weapon);
            else
                slot.Clear();
        }
    }

    private void UpdateAugmentsDisplay()
    {
        if (selectedWeapon != null)
        {
            var weaponAugments = GetCompatibleAugments(selectedWeapon);
            augmentsText.text = "Equipped Augments:\n" + string.Join("\n", weaponAugments.Select(a => a.name));
        }
        else
        {
            var charAugments = GetCharacterAugments();
            augmentsText.text = "Character Augments:\n" + string.Join("\n", charAugments.Select(a => a.name));
        }
    }

    public void SetSelectedWeapon(PlayerWeapon weapon)
    {
        selectedWeapon = weapon;
        UpdateAugmentsDisplay();
    }
}