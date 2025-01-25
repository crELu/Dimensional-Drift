using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public class ObstacleAuthor : BaseAuthor
{
    public int health;
    public bool killable;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new Obstacle
        {
            Health = health,
            Killable = killable
        });
        base.Bake(baker, entity);
    }
}

public struct Obstacle : IComponentData
{
    public int Health;
    public bool Killable;
}