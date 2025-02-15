using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class SwordWeapon: PlayerWeapon
{
    public Vector3 position;
    private float _chargeTimer;
    
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        Attack attack = new Attack{Bullets = new(), bulletStats = stats.bulletStats};
        
        attack.Bullets.Enqueue(new Bullet {position = position, rotation = Quaternion.identity, time = 0});

        return attack;
    }
}