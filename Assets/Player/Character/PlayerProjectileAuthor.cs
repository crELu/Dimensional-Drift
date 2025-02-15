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
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerProjectile());
        base.Bake(baker, entity);
    }
}

public struct PlayerProjectile : IComponentData
{
    public float Health;
    public float Damage;
}