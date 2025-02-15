using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class BombWeapon: PlayerWeapon
{
    public Vector3 position;
    private float _chargeTimer;

    public override bool Fire(PlayerManager player, bool pressed)
    {
        if (pressed)
        {
            _chargeTimer += Time.deltaTime;
        }
        else
        {
            if (_chargeTimer > BaseStats.attackDelay && base.Fire(player, true))
            {
                Debug.Log("ok");
                _chargeTimer = 0;
                return true;
            }
            _chargeTimer = 0;
        }
        return false;
    }

    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        Attack attack = new Attack{Bullets = new(), bulletStats = stats.bulletStats, projectile = Attack.ProjectileType.ChargeBasic};
        
        attack.Bullets.Enqueue(new Bullet {position = position, rotation = Maths.GetRandomRotationWithinCone(stats.accuracy, stats.accuracy), time = 0});

        return attack;
    }
}