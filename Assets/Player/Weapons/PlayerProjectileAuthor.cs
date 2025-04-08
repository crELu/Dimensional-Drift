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
                baker.AddComponent<BombTag>(entity);
                break;
        }
        base.Bake(baker, entity);
    }
}

public struct PlayerProjectile : IComponentData
{
    public int Health;
    public bool InfPierce;
}

public struct LaserTag : IComponentData
{
    public float3 Rotation;
}

public struct MeleeTag : IComponentData {}

public struct BombTag : IComponentData {}

public enum AttackType
{
    Projectile,
    Laser,
    Bomb,
    Melee
}