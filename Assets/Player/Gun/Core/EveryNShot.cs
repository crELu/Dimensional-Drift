using System.Collections.Generic;
using UnityEngine;

namespace Player.Gun.Core
{
    [CreateAssetMenu(fileName = "Jhin", menuName = "Augments/Gun/Core/Jhin")]
    public class EveryNShot: CoreAugment
    {
        public override string Id => "Jhin";
        private int _counter;
        public override AugmentType Target => AugmentType.Gun;
        public override void PostProcessing(WeaponStats stats, List<Attack> attacks)
        {
            _counter++;
            if (_counter >= (4-Stacks) * 2)
            {
                _counter = 0;
                for (int i = 0; i < attacks.Count; i++)
                {
                    var a = attacks[i];
                    a.Info.Speed *= 2;
                    a.Info.Stats.damage *= 2;
                    if (a.Projectile == Attack.ProjectileType.GunBasic) a.Projectile = Attack.ProjectileType.GunCrit;
                    if (a.Projectile == Attack.ProjectileType.GunPortal) a.Projectile = Attack.ProjectileType.GunPortalCrit;
                    attacks[i] = a;
                }
            }
        }
    }
}