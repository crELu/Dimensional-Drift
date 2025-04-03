using System.Collections.Generic;
using UnityEngine;

namespace Player.Laser.Core
{
    [CreateAssetMenu(fileName = "Minigun", menuName = "Augments/Laser/Core/Minigun")]
    public class Minigun: CoreAugment
    {
        public override string Id => "Minigun";
        public override AugmentType Target => AugmentType.Laser;
    }
}