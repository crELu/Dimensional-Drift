using System.Collections.Generic;
using UnityEngine;

namespace Player.Cannon.Core
{
    [CreateAssetMenu(fileName = "Bomb", menuName = "Augments/Cannon/Core/Bomb")]
    public class Bomb: CoreAugment
    {
        public override string Id => "Bomb";
        public override AugmentType Target => AugmentType.Cannon;
        public Vector3 shrapnelCount;
        public override void PostProcessing(WeaponStats stats, List<Attack> attacks)
        {
            for (int i = 0; i < attacks.Count; i++)
            {
                var attack = attacks[i];
                var cannon = attack.Info.Effects.Cannon.Value;
                cannon.ShrapnelCount = (int)Pick(shrapnelCount);
                attack.Info.Effects.Cannon = cannon;
                attacks[i] = attack;
            }
        }
    }
}