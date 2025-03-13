using System.Collections.Generic;
using UnityEngine;

namespace Player.Laser.Core
{
    [CreateAssetMenu(fileName = "Beam", menuName = "Augments/Laser/Core/Beam")]
    public class Beam: CoreAugment
    {
        public override string Id => "Beam";
        public override AugmentType Target => AugmentType.Laser;
    }
}