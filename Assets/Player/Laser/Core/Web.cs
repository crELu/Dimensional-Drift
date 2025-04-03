using System.Collections.Generic;
using UnityEngine;

namespace Player.Laser.Core
{
    [CreateAssetMenu(fileName = "Web", menuName = "Augments/Laser/Core/Web")]
    public class Web: CoreAugment
    {
        public override string Id => "Web";
        public override AugmentType Target => AugmentType.Laser;
    }
}