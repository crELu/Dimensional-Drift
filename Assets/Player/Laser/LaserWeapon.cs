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

    public static float3 LaserOffset;
    
    public Transform target;
    private Vector3 Position => PlayerManager.main.transform.InverseTransformPoint(target.position);
    [SerializeField] private float _chargeTimer;
    [SerializeField] private float _laserDuration, _maxDuration;
    public static bool LaserIsActive;
    
    private int _laserCount;

    protected new void Start()
    {
        base.Start();
        LaserOffset = Vector3.Scale(target.localPosition, target.parent.localScale);
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
            if (_chargeTimer > MainStats.attackDelay && base.Fire(player, true))
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
        var player = PlayerManager.main;
        base.Update();
        _laserDuration -= Time.deltaTime;
        
        if (LaserIsActive)
        {
            var cost = MainStats.ammoUse * 3 / _maxDuration * Time.deltaTime;
            if (_laserDuration >= 0 && cost <= player.Ammo)
            {
                player.UseAmmo(cost);
            } else
            {
                maxLaserCount = 0;
                _chargeTimer = 0;
                LaserIsActive = false;
            }
        }
    }
    
    protected override Attack BaseWeaponAttack(WeaponStats stats)
    {
        BaseEffects effects = new BaseEffects {Laser = new LaserEffects()};
        AttackInfo info = new AttackInfo { Stats = stats.bulletStats, Scale = new float3(stats.size, stats.size, stats.speed), Speed = 0 , Effects = effects};
        Attack attack = new Attack { Bullets = new(), Info = info, Projectile = Attack.ProjectileType.LaserBasic };
        
        if (maxLaserCount == 0)
        {
            attack.Bullets.Enqueue(new Bullet { position = Position, rotation = Vector3.zero, time = Time.time });
            attack.Bullets.Enqueue(new Bullet { position = Position, rotation = new(0, 15, 0), time = Time.time });
            _laserDuration = stats.bulletStats.duration;
            _maxDuration = _laserDuration;
            Cooldown += stats.bulletStats.duration;
        }

        return attack;
    }
}