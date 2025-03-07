using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Collider = Unity.Physics.Collider;

public class LaserWeapon : PlayerWeapon
{
    [Header("Laser Settings")]

    [SerializeField] private int maxLaserCount;
    [SerializeField] private float3 laserRotationOffset;

    public static float3 LaserOffset;
    public static float3 LaserRotationOffset;
    
    [SerializeField] private float _chargeTimer;
    [SerializeField] private float _laserDuration;
    public static bool LaserIsActive;
    
    private int _laserCount;

    protected new void Start()
    {
        base.Start();
        LaserOffset = Vector3.Scale(position.localPosition, position.parent.localScale);
        LaserRotationOffset = laserRotationOffset;
    }
    
    public override bool Fire(PlayerManager player, bool pressed)
    {
        if (pressed)
        {
            if (maxLaserCount != 0)
            {
                return false;
            }

            _chargeTimer += Time.deltaTime;
            if (_chargeTimer > BaseStats.attackDelay && base.Fire(player, true))
            {
                _chargeTimer = 0;
                
                if (!LaserIsActive)
                {
                    maxLaserCount++;
                    LaserIsActive = true;
                }
                return true;
            }
        }
        else
        {
            _chargeTimer = 0;
            maxLaserCount = 0;
            if (_laserDuration > 0)
            {
                Cooldown -= _laserDuration;
            }
            LaserIsActive = false;
        }

        return false;
    }
    
    protected new void Update()
    {
        base.Update();
        _laserDuration -= Time.deltaTime;
        if (LaserIsActive && _laserDuration <= 0)
        {
            maxLaserCount = 0;
            _chargeTimer = 0;
            LaserIsActive = false;
        }
    }
    
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        AttackInfo info = new AttackInfo() { Stats = stats.bulletStats, Scale = new float3(stats.size, stats.size, stats.speed), Speed = 0 };
        Attack attack = new Attack { Bullets = new(), Info = info, Projectile = Attack.ProjectileType.LaserBasic };
        
        if (maxLaserCount == 0)
        {
            attack.Bullets.Enqueue(new Bullet { position = position.position, rotation = Quaternion.identity, time = 0 });
            _laserDuration = stats.bulletStats.duration;
            Cooldown += stats.bulletStats.duration;
        }

        return attack;
    }
}