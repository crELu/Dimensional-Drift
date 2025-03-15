using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public class PlayerProjectileAuthor : BaseAuthor
{
    public bool infPierce;
    public AttackType attack;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerProjectile{InfPierce = infPierce});
        switch (attack)
        {
            case AttackType.Laser:
                baker.AddComponent<LaserTag>(entity);
                break;
            case AttackType.Melee:
                baker.AddComponent<MeleeTag>(entity);
                break;
            case AttackType.Bomb:
                baker.AddComponent(entity, new ExplosionPoint
                {
                    Position = float3.zero,
                    Radius = 0
                });
                break;
        }
        base.Bake(baker, entity);
    }
}

public struct PlayerProjectile : IComponentData
{
    public BulletStats Stats;
    public int Health;
    public bool InfPierce;
}

public struct LaserTag : IComponentData {}

public struct MeleeTag : IComponentData {}

public struct ExplosionPoint : ICleanupComponentData
{
    public float3 Position;
    public float Damage;
    public float Radius;
}

public enum AttackType
{
    Projectile,
    Laser,
    Bomb,
    Melee
}