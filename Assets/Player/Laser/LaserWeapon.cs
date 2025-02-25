using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class LaserWeapon: PlayerWeapon
{
    private float _chargeTimer;
    public override bool Fire(PlayerManager player, bool pressed)
    {
        if (pressed)
        {
            _chargeTimer += Time.deltaTime;
            if (_chargeTimer > BaseStats.attackDelay && base.Fire(player, true))
            {
                _chargeTimer = 0;
                return true;
            }
        }
        else
        {
            _chargeTimer = 0;
        }

        return false;
    }
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        AttackInfo info = new AttackInfo() { Stats = stats.bulletStats , Scale = new float3(stats.size, stats.size, stats.speed), Speed = 0};
        Attack attack = new Attack{Bullets = new(), Info = info, Projectile = Attack.ProjectileType.LaserBasic};
        
        attack.Bullets.Enqueue(new Bullet {position = position.position, rotation = Quaternion.identity, time = 0});

        return attack;
    }
}