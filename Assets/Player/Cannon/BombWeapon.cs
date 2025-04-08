using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class BombWeapon: PlayerWeapon
{
    private float _chargeTimer;
    public AudioSource chargeSound;
    private bool _charged;
    public Transform target;
    
    private Vector3 Position => PlayerManager.main.transform.InverseTransformPoint(target.position);
    public override bool Fire(PlayerManager player, bool pressed)
    {
        if (pressed)
        {
            _chargeTimer += Time.deltaTime;
            if (!_charged && _chargeTimer > MainStats.attackDelay)
            {
                chargeSound.Play();
                _charged = true;
            }
        }
        else
        {
            if (_charged && base.Fire(player, true))
            {
                _chargeTimer = 0;
                _charged = false;
                return true;
            }
            _charged = false;
            _chargeTimer = 0;
        }
        return false;
    }

    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        BaseEffects effects = new BaseEffects { Cannon = new CannonEffects() };
        AttackInfo info = new AttackInfo { Stats = stats.bulletStats , Scale = new float3(stats.size), Speed = stats.speed, Effects = effects};
        Attack attack = new Attack{Bullets = new(), Info = info, Projectile = Attack.ProjectileType.ChargeBasic};
        
        attack.Bullets.Enqueue(new Bullet {position = Position, rotation = new(Maths.Rand(stats.accuracy), Maths.Rand(stats.accuracy), 0), time = Time.time});

        return attack;
    }
}