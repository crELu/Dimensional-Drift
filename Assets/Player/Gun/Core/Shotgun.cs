using System.Collections.Generic;
using UnityEngine;

namespace Player.Gun.Core
{
    [CreateAssetMenu(fileName = "Shotgun", menuName = "Augments/Gun/Core/Shotgun")]
    public class Shotgun: CoreAugment
    {
        public override string Id => "Shotgun";
        public override AugmentType Target => AugmentType.Gun;
    }
}