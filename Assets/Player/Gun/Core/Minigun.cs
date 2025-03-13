using System.Collections.Generic;
using UnityEngine;

namespace Player.Gun.Core
{
    [CreateAssetMenu(fileName = "Rapidfire", menuName = "Augments/Gun/Core/Rapidfire")]
    public class Rapidfire: CoreAugment
    {
        public override string Id => "Rapidfire";
        public override AugmentType Target => AugmentType.Gun;
    }
}