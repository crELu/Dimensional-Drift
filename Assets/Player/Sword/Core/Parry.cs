﻿using UnityEngine;

namespace Player.Sword.Core
{
    [CreateAssetMenu(fileName = "Parry", menuName = "Augments/Sword/Core/Parry")]
    public class Parry: CoreAugment
    {
        public override AugmentType Target => AugmentType.Sword;
    }
}