using System.Collections.Generic;
using UnityEngine;

namespace Player.Cannon.Core
{
    [CreateAssetMenu(fileName = "Railgun", menuName = "Augments/Cannon/Core/Railgun")]
    public class Railgun: CoreAugment
    {
        public override string Id => "Railgun";
        public override AugmentType Target => AugmentType.Cannon;
    }
}