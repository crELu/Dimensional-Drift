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
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerProjectile{InfPierce = infPierce});
        base.Bake(baker, entity);
    }
}

public struct PlayerProjectile : IComponentData
{
    public BulletStats Stats;
    public int Health;
    public bool InfPierce;
}