using System;
using System.Collections;
using System.Collections.Generic;
using ECS.Enemy;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class TrailAuthor : BaseAuthor
{
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new TempTrail
        {
        });
    }
}

public struct TempTrail : IComponentData
{
}

public struct TrailTexture : ISharedComponentData, IEquatable<TrailTexture>
{
    public Texture2D Position;
    public bool Equals(TrailTexture other)
    {
        return Equals(Position, other.Position);
    }

    public override bool Equals(object obj)
    {
        return obj is TrailTexture other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position);
    }
}

public struct TrailStateSingleton : IComponentData
{
}

public struct TrailData : IComponentData
{
    public int X, Y;
}

[BurstCompile]
public partial struct TrailSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (tempTrail, entity) in SystemAPI.Query<RefRW<TempTrail>>().WithEntityAccess())
        {
            if (!TrailManager.tex) break;
            ecb.RemoveComponent<TempTrail>(entity);
            AddTrail(entity, ref ecb, ref state);
        }
        
        var uniqueSharedComponents = new List<TrailTexture>();
        state.EntityManager.GetAllUniqueSharedComponentsManaged(uniqueSharedComponents);

        foreach (var texture in uniqueSharedComponents)
        {
            if (texture.Position == null) continue;
            foreach (var (trailRef, transformRef) in SystemAPI
                         .Query<RefRO<TrailData>, RefRO<LocalTransform>>()
                         .WithSharedComponentFilterManaged(texture))
            {
                var trail = trailRef.ValueRO;
                var transform = transformRef.ValueRO;
                texture.Position.SetPixel(trail.X, trail.Y, new Color(transform.Position.x, transform.Position.y,transform.Position.z, 1));
            }
            texture.Position.Apply();
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void AddTrail(Entity e, ref EntityCommandBuffer ecb, ref SystemState state)
    {
        var data = TrailManager.main.RegisterTrail();
        ecb.AddSharedComponentManaged(e, new TrailTexture{Position = TrailManager.tex});
        ecb.AddComponent(e, new TrailData { X = data.Item1, Y = data.Item2});
    }
}