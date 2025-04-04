using System.Collections.Generic;
using UnityEngine;

namespace Player.Character.Core
{
    [CreateAssetMenu(fileName = "Gunship", menuName = "Augments/Character/Core/Gunship")]
    public class Gunship: CharacterAugment
    {
        public override AugmentType Target => AugmentType.Character;
        public override string Id => "Gunship";
        public override AllStats GetStats(AllStats prevStats)
        {
            CharacterStats stats = new CharacterStats
            {
                flatShield = -1000,
                damageMultiplier = Stacks == 3 ? prevStats.characterStats.shieldRegen * 2 : 0,
            };
            return base.GetStats(prevStats) + stats;
        }
    }
}