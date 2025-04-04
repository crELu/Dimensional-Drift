using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Player.Gun.Core
{
    [CreateAssetMenu(fileName = "Echo", menuName = "Augments/Gun/Core/Echo")]
    public class Echoes: CoreAugment
    {
        public override string Id => "Echo";
        public Vector2 echoSize;
        public override AugmentType Target => AugmentType.Gun;
        public Vector3 echoChance;
        public float interference;
        public override void PostProcessing(WeaponStats stats, List<Attack> attacks)
        {
            Attack attack = new Attack { Bullets = new(), Info = attacks[0].Info, Projectile = Attack.ProjectileType.GunPortal};
            if (Stacks >=2 ) attack.Info.Effects.Interference = interference;
            for (int i = 0; i < attacks.Count; i++)
            {
                var a = attacks[i];
                foreach (var b in a.Bullets)
                {
                    if (Random.value > Pick(echoChance))
                        continue;
                    var k = Random.insideUnitCircle.normalized * echoSize.x;
                    var pos = new Vector3(k.x, k.y, -echoSize.y);
                    attack.Bullets.Enqueue(new Bullet{rotation = Quaternion.identity, time = b.time + .1f, position = pos});
                }
            }
            attacks.Add(attack);
        }
    }
}