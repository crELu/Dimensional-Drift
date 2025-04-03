using System.Collections.Generic;
using UnityEngine;

namespace Player.Cannon.Core
{
    [CreateAssetMenu(fileName = "Cluster", menuName = "Augments/Cannon/Core/Cluster")]
    public class Cluster: CoreAugment
    {
        public override string Id => "Cluster";
        public override AugmentType Target => AugmentType.Cannon;
        public Vector3 bombCount;
        public override void PostProcessing(WeaponStats stats, List<Attack> attacks)
        {
            for (int i = 0; i < attacks.Count; i++)
            {
                var attack = attacks[i];
                var cannon = attack.Info.Effects.Cannon.Value;
                cannon.ClusterCount = (int)Pick(bombCount);
                attack.Info.Effects.Cannon = cannon;
                attacks[i] = attack;
            }
        }
    }
}