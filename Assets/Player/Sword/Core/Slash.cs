using UnityEngine;

namespace Player.Sword.Core
{
    [CreateAssetMenu(fileName = "Slash", menuName = "Augments/Sword/Core/Slash")]
    public class Slash: CoreAugment
    {
        public override AugmentType Target => AugmentType.Sword;
    }
}