using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


public class PlayerWeapon : MonoBehaviour
{
    /// <summary>
    /// A NativeArray of bullets, sorted by increasing time
    /// </summary>
    public List<Attack> Bullets = new List<Attack>();

    [SerializeField] protected WeaponStats baseStats;
    protected WeaponStats Stats;
    protected WeaponStats BaseStats;
    
    [SerializeField] protected List<StatsAugment> BasicAugments = new();
    [SerializeField] protected List<SpecializedAugment> SpecializedAugments = new();
    [SerializeField] protected List<CoreAugment> CoreAugments = new();

    private float Cd => 1 / BaseStats.attackSpeed;
    private float _cooldown;

    private void Start()
    {
        Compile();
    }

    public void Compile()
    {
        Stats = CollectStats().weaponStats;
        foreach (var core in CoreAugments)
        {
            if (core.Verify()) core.Compile(Stats);
        }

        BaseStats = baseStats * Stats;
    }

    private void Update()
    {
        _cooldown -= Time.deltaTime;
    }

    public virtual bool Fire(bool pressed)
    {
        if (pressed && _cooldown < 0)
        {
            _cooldown = Cd;
            return true;
        }
        return false;
    }

    public void RecalcBullets()
    {
        Compile();
        Bullets.Clear();
        Bullets.Add(BaseWeaponAttack(BaseStats));
        foreach (var core in CoreAugments)
        {
            var attack = core.Fire(Stats);
            if (attack.HasValue) Bullets.Add(attack.Value);
        }
        foreach (var core in CoreAugments)
        {
            core.PostProcessing(Stats, Bullets);
        }
        
    }

    protected virtual Attack BaseWeaponAttack(WeaponStats stats)
    {
        throw new System.NotImplementedException();
    }

    protected AllStats CollectStats()
    {
        AllStats s = new();
        foreach (var augment in CoreAugments)
        {
            s += augment.GetStats();
        }
        foreach (var augment in BasicAugments)
        {
            s += augment.GetStats();
        }
        foreach (var augment in SpecializedAugments)
        {
            s += augment.GetStats();
        }

        return s;
    }
}

[Serializable]
public struct Attack
{
    public Queue<Bullet> Bullets;
    public float damage, speed, lifetime;
    public ProjectileType projectile;
    
    public enum ProjectileType
    {
        GunBasic,
        GunCrit,
        GunPlaceholder1,
        GunPlaceholder2,
        
        ChargeBasic,
        ChargeNuke,
        ChargeRockets,
        ChargeShrapnel,
        ChargePlaceholder1,
        ChargePlaceholder2,
        
        LaserBasic,
        LaserBigBeam,
        LaserRefracted,
        LaserBall,
        LaserPlaceholder1,
        LaserPlaceholder2,
    }
}


[Serializable]
public struct Bullet
{
    public Vector3 position;
    public Quaternion rotation;
    // delay until firing
    public float time;
}

