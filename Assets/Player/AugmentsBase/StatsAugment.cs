using UnityEngine;

// Can pick for any weapon, stats changes only
[CreateAssetMenu(fileName = "WeaponStats", menuName = "Augments/Basic/Weapon")]
public class StatsAugment: Augment
{
    public override string Id => "";
    
    public AugmentType target;
    public WeaponStats stats;
    public override AugmentType Target => target;
    
    public override AllStats GetStats(AllStats prevStats)
    {
        return new AllStats{weaponStats = stats};
    }
}
