using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class LaserWeapon: PlayerWeapon
{
    public Vector3 position;
    private float _chargeTimer;
    public override bool Fire(bool pressed)
    {
        if (pressed)
        {
            _chargeTimer += Time.deltaTime;
            if (_chargeTimer > BaseStats.attackDelay && base.Fire(true))
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
        Attack attack = new Attack{Bullets = new(), speed = stats.speed, lifetime = stats.duration, damage = stats.damage};
        
        attack.Bullets.Enqueue(new Bullet {position = position, rotation = Quaternion.identity, time = 0});

        return attack;
    }
}