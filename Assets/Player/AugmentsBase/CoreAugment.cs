using System.Collections.Generic;
using UnityEngine;

// Pick 3 per weapon
public class CoreAugment: Augment
{
    public override int MaxStacks => 3;
    
    public WeaponStats tier1, tier2, tier3; // extra stats per level (applies to the entire weapon)
    public int postPriority;
    
    protected Attack MainAttack;
    public override bool Verify()
    {
        if (MaxStacks > 3)
        {
            Debug.Log("Too many max stacks for a core augment");
            return false;
        }
        return base.Verify();
    }

    protected float Pick(Vector3 values)
    {
        return Stacks == 3 ? values.z : Stacks == 2 ? values.y : values.x;
    }

    public override AllStats GetStats(AllStats prevStats)
    {
        WeaponStats states = tier1;
        if (Stacks == 2) states += tier2;
        if (Stacks == 3) states += tier3;
        return new AllStats {weaponStats = states };
    }

    public virtual void Compile(WeaponStats stats)
    {
    }
    
    public virtual Attack? Fire(WeaponStats stats)
    {
        return null;
    }

    public virtual void PostProcessing(WeaponStats stats, List<Attack> attacks)
    {
    }
}