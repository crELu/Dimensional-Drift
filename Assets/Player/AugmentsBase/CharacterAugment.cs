using System.Collections.Generic;
using UnityEngine;

// Pick 3 per weapon
public class CharacterAugment: Augment
{
    public override int MaxStacks => 3;

    public override AugmentType Target => AugmentType.Character;
    public CharacterStats tier1, tier2, tier3; // extra stats per level (applies to the entire weapon)
    
    public override bool Verify()
    {
        if (MaxStacks > 3)
        {
            Debug.Log("Too many max stacks for a character core augment");
            return false;
        }
        return base.Verify();
    }

    public override AllStats GetStats()
    {
        CharacterStats states = tier1;
        if (Stacks == 2) states += tier2;
        if (Stacks == 3) states += tier3;
        return new AllStats {characterStats = states };
    }
}