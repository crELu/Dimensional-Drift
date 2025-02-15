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
            if (_counter >= (4-Stacks) * 2)
            {
                _counter = 0;
                for (int i = 0; i < attacks.Count; i++)
                {
                    var a = attacks[i];
                    a.bulletStats.speed *= 2;
                    a.bulletStats.damage *= 2;
                    a.projectile = Attack.ProjectileType.GunCrit;
                    attacks[i] = a;
                }
            }
        }
    }
}