using System.Collections.Generic;
using UnityEngine;

namespace Player.Laser.Core
{
    [CreateAssetMenu(fileName = "Interference", menuName = "Augments/Laser/Core/Interference")]
    public class Interference: CoreAugment
    {
        public override string Id => "Interference";
        public override AugmentType Target => AugmentType.Laser;
    }
}