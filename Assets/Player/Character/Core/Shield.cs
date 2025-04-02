using System.Collections.Generic;
using UnityEngine;

namespace Player.Character.Core
{
    [CreateAssetMenu(fileName = "Shield", menuName = "Augments/Character/Core/Shield")]
    public class Shield: CharacterAugment
    {
        public override AugmentType Target => AugmentType.Character;
        
        public override AllStats GetStats(AllStats prevStats)
        {
            bool cool = Stacks == 3 && PlayerManager.main.FullShield && PlayerManager.main.FullHealth;
            CharacterStats stats = new CharacterStats
            {
                flatShield = -1000,
                damageMultiplier = Stacks == 3 ? prevStats.characterStats.shieldRegen : 0,
            };
            return base.GetStats(prevStats) + stats;
        }
    }
}