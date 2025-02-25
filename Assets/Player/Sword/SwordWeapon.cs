using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class SwordWeapon: PlayerWeapon
{
    private float _chargeTimer;
    
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        AttackInfo info = new AttackInfo() { Stats = stats.bulletStats , Scale = new float3(stats.size), Speed = 0};
        Attack attack = new Attack{Bullets = new(), Info = info};
        
        attack.Bullets.Enqueue(new Bullet {position = position.position, rotation = Quaternion.identity, time = 0});

        return attack;
    }
}