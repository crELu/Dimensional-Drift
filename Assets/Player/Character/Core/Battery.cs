using System.Collections.Generic;
using UnityEngine;

namespace Player.Character.Core
{
    [CreateAssetMenu(fileName = "Battery", menuName = "Augments/Character/Core/Battery")]
    public class Battery: CharacterAugment
    {
        public override AugmentType Target => AugmentType.Character;
        public override string Id => "Battery";
        public override AllStats GetStats(AllStats prevStats)
        {
            CharacterStats stats = new CharacterStats
            {
                damageMultiplier = Stacks >= 2 ? PlayerManager.main.Ammo / 10 : 0,
                ammoRegen = Stacks == 3 && Mathf.Approximately(PlayerManager.main.shield, PlayerManager.main.MaxShield) ? 100 : 0,
            };
            return base.GetStats(prevStats) + stats;
        }
    }
}