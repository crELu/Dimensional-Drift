using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public List<Attack> Bullets = new ();

    [SerializeField] protected WeaponStats baseStats;
    protected WeaponStats Stats;
    protected WeaponStats MainStats;
    protected WeaponStats AddonStats;
    
    [SerializeField] protected List<SpecializedAugment> SpecializedAugments = new();
    [SerializeField] protected List<CoreAugment> CoreAugments = new();

    [SerializeField] private AudioSource WeaponTrack;
    [SerializeField] private AudioClip WeaponSFX;
    
    [SerializeField] private AugmentType weaponType;
    public AugmentType WeaponType => weaponType;

    private float Cd => 1 / MainStats.attackSpeed;
    protected float Cooldown;

    protected void Start()
    {
        Compile();
    }

    public void Compile()
    {
        Stats = AddonStats + CollectStats().weaponStats;
        foreach (var core in CoreAugments)
        {
            if (core.Verify()) core.Compile(Stats);
        }

        MainStats = baseStats * Stats;
    }

    protected void Update()
    {
        Cooldown -= Time.deltaTime;
    }

    public virtual bool Fire(PlayerManager player, bool pressed)
    {
        Compile();
        
        if (pressed && Cooldown < 0 && MainStats.ammoUse <= player.Ammo)
        {
            Cooldown = Cd;
            player.UseAmmo(MainStats.ammoUse);
            RecalcBullets();

            WeaponTrack.PlayOneShot(WeaponSFX);
            return true;
        }
        return false;
    }

    public void RecalcBullets()
    {
        Compile(); // TODO remove this at some point when we can guarantee this runs after anything is changed
        List<Attack> current = new ();
        current.Add(BaseWeaponAttack(MainStats));
        foreach (var core in CoreAugments)
        {
            var attack = core.Fire(Stats);
            if (attack.HasValue) current.Add(attack.Value);
        }
        foreach (var core in CoreAugments.OrderBy(c => -c.postPriority))
        {
            core.PostProcessing(Stats, current);
        }
        Bullets.AddRange(current);
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
            s += augment.GetStats(new AllStats{weaponStats = MainStats});
        }
        foreach (var augment in SpecializedAugments)
        {
            s += augment.GetStats(new AllStats{weaponStats = MainStats});
        }

        return s;
    }

    public void AddAugment(Augment augment)
    {
        if (augment.Target != weaponType)
        {
            Debug.Log($"Wrong augment type {augment.Target} for weaponType {weaponType}.");
        }
        if (augment is CoreAugment coreAug)
            CoreAugments.Add(coreAug);
        else if (augment is SpecializedAugment specAug)
            SpecializedAugments.Add(specAug);
        else if (augment is StatsAugment statsAug)
            AddonStats += statsAug.GetStats(new AllStats{weaponStats = MainStats}).weaponStats;
    }
}

public struct AttackInfo
{
    public float3 Scale;
    public float Speed;
    public BulletStats Stats;
    public BaseEffects Effects;
}

public struct Attack
{
    public Queue<Bullet> Bullets;
    public AttackInfo Info;
    public ProjectileType Projectile;
    public enum ProjectileType
    {
        GunBasic,
        GunCrit,
        GunPortal,
        GunPortalCrit,
        
        ChargeBasic,
        ChargeNuke,
        ChargeRockets,
        ChargeShrapnel,
        ChargeRecursive,
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

