using System.Collections.Generic;
using UnityEngine;

namespace Player.Character.Core
{
    [CreateAssetMenu(fileName = "Research", menuName = "Augments/Character/Core/Research")]
    public class Research: CharacterAugment
    {
        public override AugmentType Target => AugmentType.Character;
        public override string Id => "Research";
    }
}