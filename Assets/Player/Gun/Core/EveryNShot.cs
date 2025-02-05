using System.Collections.Generic;
using UnityEngine;

namespace Player.Gun.Core
{
    [CreateAssetMenu(fileName = "Jhin", menuName = "Augments/Gun/Core/Jhin")]
    public class EveryNShot: CoreAugment
    {
        private int _counter;

        public override void PostProcessing(WeaponStats stats, List<Attack> attacks)
        {
            _counter++;
            if (_counter >= (Stacks == 3 ? 3 : 6))
            {
                _counter = 0;
                for (int i = 0; i < attacks.Count; i++)
                {
                    var a = attacks[i];
                    a.speed *= 2;
                    a.damage *= 2;
                    a.projectile = Attack.ProjectileType.GunCrit;
                    attacks[i] = a;
                }
            }
        }
    }
}