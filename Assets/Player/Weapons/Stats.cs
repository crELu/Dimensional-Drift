using System;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public struct WeaponStats
{
    public float size;          // projectile size
    public float speed;         // projectile speed
    public float attackSpeed;   // attack speed
    public float attackDelay;   // either charge time or time between attacks
    public float accuracy;      // how spread out attacks are
    public float ammoUse;       // how much ammo is used
    public int count;           // extra projectiles
    public BulletStats bulletStats;
    public static WeaponStats operator +(WeaponStats a, WeaponStats b)
    {
        return new WeaponStats
        {
            size = Maths.CompoundPercentage(a.size, b.size),
            speed = Maths.CompoundPercentage(a.speed, b.speed),
            attackSpeed = Maths.CompoundPercentage(a.attackSpeed, b.attackSpeed),
            attackDelay = Maths.CompoundPercentage(a.attackDelay, b.attackDelay),
            ammoUse = Maths.CompoundPercentage(a.ammoUse, b.ammoUse),
            accuracy = Maths.CompoundPercentage(a.accuracy, b.accuracy),
            count = a.count + b.count,
            bulletStats = a.bulletStats + b.bulletStats,
        };
    }
    
    public static WeaponStats operator *(WeaponStats a, WeaponStats b)
    {
        return new WeaponStats
        {
            size = a.size * (1 + b.size / 100),
            speed = a.speed * (1 + b.speed / 100),
            attackSpeed = a.attackSpeed * (1 + b.attackSpeed / 100),
            attackDelay = a.attackDelay * (1 + b.attackDelay / 100),
            ammoUse = a.ammoUse / (1 + b.ammoUse / 100),
            accuracy = a.accuracy / (1 + b.accuracy / 100),
            count = a.count + b.count,
            bulletStats = a.bulletStats * b.bulletStats,
        };
    }
    
    public string Relative => $"AttackSpeed: {attackSpeed:F2}%, Size: {size:F2}%, Speed: {speed:F2}%, " +
                              $"AttackDelay: {attackDelay:F2}%, " +
                              $"Count: {count}x, Accuracy: {accuracy:F2}%, Ammo: {ammoUse:F2}%";
    public string Absolute => $"AttackSpeed: {attackSpeed:F2} attacks/s, Size: {size:F2}, Speed: {speed:F2}, " +
                              $"AttackDelay: {attackDelay:F2}s, " +
                              $"Count: {count}x, Accuracy: {accuracy:F2} deg, Ammo: -{ammoUse:F2}%";
    
    public static implicit operator AllStats(WeaponStats stats) => new() { weaponStats = stats };
}

[Serializable]
public struct BulletStats
{
    public float damage;        // extra damage
    
    public float duration;      // lifespan of projectiles
    public int extraction;      // how much bonus intel enemies drop
    public int power;           // projectile destroying ability
    public int pierce;          // how many enemies are pierced
    
    public static BulletStats operator +(BulletStats a, BulletStats b)
    {
        return new BulletStats
        {
            damage = Maths.CompoundPercentage(a.damage, b.damage),
            
            duration = Maths.CompoundPercentage(a.duration, b.duration),
            power = a.power + b.power,
            pierce = a.pierce + b.pierce,
            extraction = a.extraction + b.extraction
        };
    }
    
    public static BulletStats operator *(BulletStats a, BulletStats b)
    {
        return new BulletStats
        {
            damage = a.damage * (1 + b.damage / 100),
            duration = a.duration * (1 + b.duration / 100),
            power = a.power + b.power,
            pierce = a.pierce + b.pierce,
            extraction = a.extraction + b.extraction
        };
    }
    
    public string Relative => $"Damage: {damage:F2}%, " +
                              $"Duration: {duration:F2}%, " +
                              $"Precision: {extraction}" +
                              $"Power: {power}x, Pierce: {pierce}";
    public string Absolute => $"Damage: {damage:F2}, " +
                              $"Duration: {duration:F2}s, " +
                              $"Precision: {extraction}" +
                              $"Power: {power}x, Pierce: {pierce}";
    
}

[Serializable]
public struct CharacterStats
{
    public float flatHealth;
    public float flatShield;
    public float percentHealth;
    public float percentShield;
    public float shieldRegen;
    public float boostRegen;
    public float ammoRegen;
    public float flatAmmo;
    public float dashCd;
    public float pickupRadius;
    public float damageMultiplier;
    
    public static CharacterStats operator +(CharacterStats a, CharacterStats b)
    {
        return new CharacterStats
        {
            flatHealth = a.flatHealth + b.flatHealth,
            flatShield = a.flatShield + b.flatShield,
            percentHealth = a.percentHealth + b.percentHealth,
            percentShield = a.percentShield + b.percentShield,
            shieldRegen = Maths.CompoundPercentage(a.shieldRegen, b.shieldRegen),
            boostRegen = Maths.CompoundPercentage(a.boostRegen, b.boostRegen),
            ammoRegen = Maths.CompoundPercentage(a.ammoRegen, b.ammoRegen),
            flatAmmo = a.flatAmmo + b.flatAmmo,
            dashCd = Maths.CompoundPercentage(a.dashCd, b.dashCd),
            pickupRadius = Maths.CompoundPercentage(a.pickupRadius, b.pickupRadius),
            damageMultiplier = Maths.CompoundPercentage(a.damageMultiplier, b.damageMultiplier),
        };
    }
    
    public static CharacterStats operator *(CharacterStats a, CharacterStats b)
    {
        return new CharacterStats
        {
            flatHealth = Mathf.Max((int)((a.flatHealth + b.flatHealth) * (1 + a.percentHealth / 100f) * (1 + b.percentHealth / 100f)), 1),
            flatShield = Mathf.Max((int)((a.flatShield + b.flatShield) * (1 + a.percentShield / 100f) * (1 + b.percentShield / 100f)), 1),
            percentHealth = a.percentHealth * (1 + b.percentHealth / 100),
            percentShield = a.percentShield * (1 + b.percentShield / 100),
            shieldRegen = a.shieldRegen * (1 + b.shieldRegen / 100),
            boostRegen = a.boostRegen * (1 + b.boostRegen / 100),
            ammoRegen = a.ammoRegen * (1 + b.ammoRegen / 100),
            flatAmmo = a.flatAmmo + b.flatAmmo,
            dashCd = a.dashCd * (1 + b.dashCd / 100),
            pickupRadius = a.pickupRadius * (1 + b.pickupRadius / 100),
            damageMultiplier = a.damageMultiplier * (1 + b.damageMultiplier / 100),
        };
    }
    
    public string Relative => $"Health: ({flatHealth}, {percentHealth:F2}%), Health: ({flatShield}, x{percentShield:F2}%)" +
                              $"MoveSpeed: {boostRegen:F2}%, Shield Regen: {shieldRegen:F2}%, " +
                              $"DashCd: {dashCd}%, PickupRadius: {pickupRadius:F2}%";

    public static implicit operator AllStats(CharacterStats stats) => new() { characterStats = stats };
}

[Serializable]
public struct AllStats
{
    public WeaponStats weaponStats;
    public CharacterStats characterStats;
    
    public static AllStats operator +(AllStats a, AllStats b)
    {
        return new AllStats
        {
            weaponStats = a.weaponStats + b.weaponStats,
            characterStats = a.characterStats + b.characterStats,
        };
    }
    
}