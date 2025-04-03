using UnityEngine;

// Can pick for any weapon, stats changes only
[CreateAssetMenu(fileName = "CharStats", menuName = "Augments/Basic/Character")]
public class StatsCharAugment: Augment
{
    public override string Id => "";
    
    public AugmentType target;
    public CharacterStats stats;
    public override AugmentType Target => target;
    public override AllStats GetStats()
    {
        return new AllStats{characterStats = stats};
    }
}
