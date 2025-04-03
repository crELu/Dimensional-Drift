using UnityEngine;

namespace Player.Sword.Core
{
    [CreateAssetMenu(fileName = "Wave", menuName = "Augments/Sword/Core/Wave")]
    public class Wave: CoreAugment
    {
        public override AugmentType Target => AugmentType.Sword;
    }
}