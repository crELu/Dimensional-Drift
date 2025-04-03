using System.Collections.Generic;
using UnityEngine;

namespace Player.Laser.Core
{
    [CreateAssetMenu(fileName = "Refract", menuName = "Augments/Laser/Core/Refract")]
    public class Refract: CoreAugment
    {
        public override string Id => "Refract";
        public override AugmentType Target => AugmentType.Laser;
    }
}