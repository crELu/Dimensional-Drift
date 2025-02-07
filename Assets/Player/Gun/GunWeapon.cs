using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class GunWeapon: PlayerWeapon
{
    public Vector3 position;

    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        Attack attack = new Attack{Bullets = new(), speed = stats.speed, lifetime = stats.duration, damage = stats.damage, projectile = Attack.ProjectileType.GunBasic};
        
        float weaponCd = 1 / stats.attackSpeed;
        float delayAmount = Maths.Sigmoid(stats.attackDelay);
        int count = Mathf.Max(1 + stats.count, 1);
        float attackCd = weaponCd / count * delayAmount;
        
        for (int i = 0; i < count; i++)
        {
            attack.Bullets.Enqueue(new Bullet {position = position, rotation = Maths.GetRandomRotationWithinCone(stats.accuracy, stats.accuracy), time = i * attackCd});
        }

        return attack;
    }
}