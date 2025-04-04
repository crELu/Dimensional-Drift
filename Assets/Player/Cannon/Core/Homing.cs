using System.Collections.Generic;
using UnityEngine;

namespace Player.Cannon.Core
{
    [CreateAssetMenu(fileName = "Homing", menuName = "Augments/Cannon/Core/Homing")]
    public class Homing: CoreAugment
    {
        public override string Id => "Homing";
        public override AugmentType Target => AugmentType.Cannon;
        public Vector3 RocketCount;
        public Vector3 HomingCount;
        public override void PostProcessing(WeaponStats stats, List<Attack> attacks)
        {
            for (int i = 0; i < attacks.Count; i++)
            {
                var attack = attacks[i];
                attack.Info.Effects.HomingSpeed = Pick(HomingCount);
                attack.Info.Effects.HomingRadius = 50;
                var cannon = attack.Info.Effects.Cannon.Value;
                cannon.RocketCount = (int)Pick(RocketCount);
                attack.Info.Effects.Cannon = cannon;
                attacks[i] = attack;
            }
        }
    }
}