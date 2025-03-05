using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] public int maxWeaponSlots = 3;
    [SerializeField] private Transform weaponSlotsContainer;
    public Dictionary<AugmentType, PlayerWeapon> equippedWeapons = new();
    public List<Augment> ownedAugments = new();
    
    public float Intel { get; private set; }

    public void Start()
    {
        Intel = 100;
    }

    public bool CanEquipWeapon(AugmentType weaponType)
    {
        return equippedWeapons.Count < maxWeaponSlots && !equippedWeapons.ContainsKey(weaponType);
    }

    public void EquipWeapon(PlayerWeapon weapon)
    {
        if (!CanEquipWeapon(weapon.WeaponType)) return;
        equippedWeapons[weapon.WeaponType] = weapon;
    }

    public void UnequipWeapon(AugmentType weaponType)
    {
        if (equippedWeapons.ContainsKey(weaponType))
        {
            equippedWeapons.Remove(weaponType);
        }
    }

    public void AddAugment(Augment augment)
    {
        ownedAugments.Add(augment);
    }

    public bool HasAugment(Augment augment, int minTier)
    {
        return ownedAugments.Any(a => a.GetType() == augment.GetType() && a.Stacks >= minTier);
    }

    // Helper method to get augments that can be applied to a specific weapon
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

    // Helper method to get character augments
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

    public void AddIntel(float amount) => Intel += amount;

    public bool SpendIntel(float amount)
    {
        if (Intel >= amount)
        {
            Intel -= amount;
            return true;
        }
        return false;
    }


} 