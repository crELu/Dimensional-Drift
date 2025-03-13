using System.Collections.Generic;
using UnityEngine;

namespace Player.Cannon.Core
{
    [CreateAssetMenu(fileName = "Homing", menuName = "Augments/Cannon/Core/Homing")]
    public class Homing: CoreAugment
    {
        public override string Id => "Homing";
        public override AugmentType Target => AugmentType.Cannon;
    }
}