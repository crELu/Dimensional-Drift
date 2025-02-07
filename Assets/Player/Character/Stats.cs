using System;

[Serializable]
public struct WeaponStats
{
    public float damage;        // extra damage
    public float size;          // projectile size
    public float speed;         // projectile speed
    public float attackSpeed;   // attack speed
    public float attackDelay;   // either charge time or time between attacks
    public float duration;      // lifespan of projectiles
    public float accuracy;      // how spread out attacks are
    public int count;           // extra projectiles
    public int precision;     // how much bonus intel enemies drop
    
    public static WeaponStats operator +(WeaponStats a, WeaponStats b)
    {
        return new WeaponStats
        {
            damage = Maths.CompoundPercentage(a.damage, b.damage),
            size = Maths.CompoundPercentage(a.size, b.size),
            speed = Maths.CompoundPercentage(a.speed, b.speed),
            attackSpeed = Maths.CompoundPercentage(a.attackSpeed, b.attackSpeed),
            attackDelay = Maths.CompoundPercentage(a.attackDelay, b.attackDelay),
            duration = Maths.CompoundPercentage(a.duration, b.duration),
            count = a.count + b.count,
            accuracy = Maths.CompoundPercentage(a.accuracy, b.accuracy),
            precision = a.precision + b.precision
        };
    }
    
    public static WeaponStats operator *(WeaponStats a, WeaponStats b)
    {
        return new WeaponStats
        {
            damage = a.damage * (1 + b.damage / 100),
            size = a.size * (1 + b.size / 100),
            speed = a.speed * (1 + b.speed / 100),
            attackSpeed = a.attackSpeed * (1 + b.attackSpeed / 100),
            attackDelay = a.attackDelay * (1 + b.attackDelay / 100),
            duration = a.duration * (1 + b.duration / 100),
            count = a.count + b.count,
            accuracy = a.accuracy / (1 + b.accuracy / 100),
            precision = a.precision + b.precision
        };
    }
    
    public string Relative => $"Damage: {damage:F2}%, Size: {size:F2}%, Speed: {speed:F2}%, " +
                              $"AttackSpeed: {attackSpeed:F2}%, AttackDelay: {attackDelay:F2}%, Duration: {duration:F2}%, " +
                              $"Accuracy: {accuracy}%, Count: {count}x, Precision: {precision}";
    public string Absolute => $"Damage: {damage:F2}, Size: {size:F2}, Speed: {speed:F2}, " +
                              $"AttackSpeed: {attackSpeed:F2} attacks/s, AttackDelay: {attackDelay:F2}s, Duration: {duration:F2}s, " +
                              $"Accuracy: {accuracy:F2} deg, Count: {count}x, Precision: {precision}";
    
}

[Serializable]
public struct CharacterStats
{
    public int flatHealth;
    public int flatShield;
    public float percentHealth;
    public float percentShield;
    public float moveSpeed;
    public float contactDamage;
    public float dashCd;
    public float pickupRadius;
    
    public static CharacterStats operator +(CharacterStats a, CharacterStats b)
    {
        return new CharacterStats
        {
            flatHealth = a.flatHealth + b.flatHealth,
            flatShield = a.flatShield + b.flatShield,
            percentHealth = Maths.CompoundPercentage(a.percentHealth, b.percentHealth),
            percentShield = Maths.CompoundPercentage(a.percentShield, b.percentShield),
            moveSpeed = Maths.CompoundPercentage(a.moveSpeed, b.moveSpeed),
            contactDamage = Maths.CompoundPercentage(a.contactDamage, b.contactDamage),
            dashCd = Maths.CompoundPercentage(a.dashCd, b.dashCd),
            pickupRadius = Maths.CompoundPercentage(a.pickupRadius, b.pickupRadius),
        };
    }
    
    public static CharacterStats operator *(CharacterStats a, CharacterStats b)
    {
        return new CharacterStats
        {
            flatHealth = (int)((a.flatHealth + b.flatHealth) * Maths.CompoundPercentage(a.percentHealth, b.percentHealth)),
            flatShield = (int)((a.flatShield + b.flatShield) * Maths.CompoundPercentage(a.percentShield, b.percentShield)),
            percentHealth = a.percentHealth * (1 + b.percentHealth / 100),
            percentShield = a.percentShield * (1 + b.percentShield / 100),
            moveSpeed = a.moveSpeed * (1 + b.moveSpeed / 100),
            contactDamage = a.contactDamage * (1 + b.contactDamage / 100),
            dashCd = a.dashCd * (1 + b.dashCd / 100),
            pickupRadius = a.pickupRadius * (1 + b.pickupRadius / 100),
        };
    }
    
    public string Relative => $"Health: ({flatHealth}, {percentHealth:F2}%), Health: ({flatShield}, x{percentShield:F2}%)" +
                              $"MoveSpeed: {moveSpeed:F2}%, ContactDamage: {contactDamage:F2}%, " +
                              $"DashCd: {dashCd}%, PickupRadius: {pickupRadius:F2}%";
    
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