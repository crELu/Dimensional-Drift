using System.Collections.Generic;
using UnityEngine;

namespace Player.Cannon.Core
{
    [CreateAssetMenu(fileName = "Cluster", menuName = "Augments/Cannon/Core/Cluster")]
    public class Cluster: CoreAugment
    {
        public override string Id => "Cluster";
        public override AugmentType Target => AugmentType.Cannon;
    }
}