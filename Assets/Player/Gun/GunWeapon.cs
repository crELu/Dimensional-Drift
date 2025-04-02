using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class GunWeapon: PlayerWeapon
{
    public Transform targetL, targetR;
    private Vector3 PositionL => PlayerManager.main.transform.InverseTransformPoint(targetL.position);
    private Vector3 PositionR => PlayerManager.main.transform.InverseTransformPoint(targetR.position);
    
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        BaseEffects effects = new BaseEffects();
        AttackInfo info = new AttackInfo { Stats = stats.bulletStats , Scale = new float3(stats.size), Speed = stats.speed, Effects = effects};
        Attack attack = new Attack{Bullets = new(), Info = info, Projectile = Attack.ProjectileType.GunBasic};
        
        float weaponCd = 1 / stats.attackSpeed;
        float delayAmount = Maths.Sigmoid(stats.attackDelay);
        int count = Mathf.Max(1 + stats.count, 1);
        float attackCd = weaponCd / count * delayAmount;
        var p1 = PositionL;
        var p2 = PositionR;
        for (int i = 0; i < count; i++)
        {
            attack.Bullets.Enqueue(new Bullet {position = p1, rotation = Maths.GetRandomRotationWithinCone(stats.accuracy, stats.accuracy), time = Time.time});
            attack.Bullets.Enqueue(new Bullet {position = p2, rotation = Maths.GetRandomRotationWithinCone(stats.accuracy, stats.accuracy), time = Time.time});

        }

        return attack;
    }
}