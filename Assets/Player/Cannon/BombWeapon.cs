using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class BombWeapon: PlayerWeapon
{
    public Vector3 position;
    private float _chargeTimer;

    public override bool Fire(bool pressed)
    {
        if (pressed)
        {
            _chargeTimer += Time.deltaTime;
        }
        else
        {
            if (_chargeTimer > Stats.attackDelay && base.Fire(true))
            {
                _chargeTimer = 0;
                return true;
            }
            _chargeTimer = 0;
            
        }

        return false;
    }

    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        Attack attack = new Attack{Bullets = new(), speed = stats.speed, lifetime = stats.duration, damage = stats.damage};
        
        int count = Mathf.Max(1 + stats.count, 1);
        for (int i = 0; i < count; i++)
        {
            attack.Bullets.Enqueue(new Bullet {position = position, rotation = Maths.GetRandomRotationWithinCone(stats.accuracy, stats.accuracy), time = 0});
        }

        return attack;
    }
}