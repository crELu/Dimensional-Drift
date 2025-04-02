using System.Collections.Generic;
using UnityEngine;

namespace Player.Character.Core
{
    [CreateAssetMenu(fileName = "Engine", menuName = "Augments/Character/Core/Engine")]
    public class Engine: CharacterAugment
    {
        public override AugmentType Target => AugmentType.Character;
        
        public override AllStats GetStats(AllStats prevStats)
        {
            CharacterStats stats = new CharacterStats
            {
                damageMultiplier = Stacks == 3 ? PlayerManager.main.velocity / 5 : 0,
            };
            return base.GetStats(prevStats) + stats;
        }
    }
}