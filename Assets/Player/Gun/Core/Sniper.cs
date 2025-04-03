using System.Collections.Generic;
using UnityEngine;

namespace Player.Gun.Core
{
    [CreateAssetMenu(fileName = "Sniper", menuName = "Augments/Gun/Core/Sniper")]
    public class Sniper: CoreAugment
    {
        public override string Id => "Sniper";
        public override AugmentType Target => AugmentType.Gun;
    }
}