using System.Collections.Generic;
using UnityEngine;

namespace Player.Character.Core
{
    [CreateAssetMenu(fileName = "Hull", menuName = "Augments/Character/Core/Hull")]
    public class Hull: CharacterAugment
    {
        public override AugmentType Target => AugmentType.Character;
        public override string Id => "Hull";
        public override AllStats GetStats(AllStats prevStats)
        {
            if (Stacks == 3 && PlayerManager.main.health < PlayerManager.main.MaxHealth / 2)
            {
                
            }
            return base.GetStats(prevStats);
        }
    }
}