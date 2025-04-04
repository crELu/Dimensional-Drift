using System.Collections.Generic;
using UnityEngine;

namespace Player.Laser.Core
{
    [CreateAssetMenu(fileName = "Minigun", menuName = "Augments/Laser/Core/Minigun")]
    public class Minigun: CoreAugment
    {
        public override string Id => "Minigun";
        public override AugmentType Target => AugmentType.Laser;
        public int stacks;
        public float stackTime;
        
        public override Attack? Fire(WeaponStats stats)
        {
            if (Time.time - stackTime < 1) stacks++;
            else stacks = 1;
            stackTime = Time.time;
            return null;
        }

        public override AllStats GetStats(AllStats prevStats)
        {
            if (Time.time - stackTime > 1) stacks = 0;
            
            return base.GetStats(prevStats) + new AllStats{weaponStats = new WeaponStats
            {
                accuracy = -stacks * 5,
                attackSpeed = stacks * 5,
                attackDelay = -100 * (1-Mathf.Pow(.95f, stacks)),
                ammoUse = -100 * (1-Mathf.Pow(.975f, stacks)),
                bulletStats = new BulletStats
                {
                    damage = stacks * 5,
                }
            }};
        }
    }
}