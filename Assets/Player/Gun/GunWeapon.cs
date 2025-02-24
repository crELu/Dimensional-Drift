using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class GunWeapon: PlayerWeapon
{
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        AttackInfo info = new AttackInfo() { Stats = stats.bulletStats , Scale = new float3(stats.size), Speed = stats.speed};
        Attack attack = new Attack{Bullets = new(), Info = info, Projectile = Attack.ProjectileType.GunBasic};
        
        float weaponCd = 1 / stats.attackSpeed;
        float delayAmount = Maths.Sigmoid(stats.attackDelay);
        int count = Mathf.Max(1 + stats.count, 1);
        float attackCd = weaponCd / count * delayAmount;
        
        for (int i = 0; i < count; i++)
        {
            attack.Bullets.Enqueue(new Bullet {position = position.position, rotation = Maths.GetRandomRotationWithinCone(stats.accuracy, stats.accuracy), time = i * attackCd});
        }

        return attack;
    }
}