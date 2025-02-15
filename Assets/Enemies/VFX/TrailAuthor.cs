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
    [ColorUsage(false, true)] public Color color = Color.magenta;
    public float lifetime = 1;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        Color c = color;
        c.a = lifetime;
        baker.AddComponent(entity, new TempTrail
        {
            ColorLife = c
        });
    }
}

public struct TempTrail : IComponentData
{
    public Color ColorLife;
}

public struct TrailTexture : ICleanupSharedComponentData, IEquatable<TrailTexture>
{
    public Texture2D Position;
    public Texture2D ColorLife;
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
    public bool Destroy;
}

public struct TrailSystemData : ICleanupComponentData
{
    public int X, Y;
    public Color ColorLife;
}

// [BurstCompile]
public partial struct TrailSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state) { }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (tempTrail, entity) in SystemAPI.Query<RefRW<TempTrail>>().WithEntityAccess())
        {
            if (!TrailManager.tex1) break;
            ecb.RemoveComponent<TempTrail>(entity);
            AddTrail(entity, ref ecb, ref state, tempTrail.ValueRO);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        
        ecb = new EntityCommandBuffer(Allocator.Temp);
        var uniqueSharedComponents = new List<TrailTexture>();
        state.EntityManager.GetAllUniqueSharedComponentsManaged(uniqueSharedComponents);

        foreach (var texture in uniqueSharedComponents)
        {
            if (texture.Position == null) continue;
            
            foreach (var (trailRef, transformRef) in SystemAPI
                         .Query<RefRO<TrailSystemData>, RefRO<LocalTransform>>().WithAll<TrailData>()
                         .WithSharedComponentFilterManaged(texture))
            {
                var trail = trailRef.ValueRO;
                var transform = transformRef.ValueRO;
                texture.Position.SetPixel(trail.X, trail.Y, new Color(transform.Position.x, transform.Position.y,transform.Position.z, 1));
                texture.ColorLife.SetPixel(trail.X, trail.Y, trail.ColorLife);
            }
            
            foreach (var (trailRef, entity) in SystemAPI.Query<RefRO<TrailSystemData>>().WithAbsent<TrailData>()
                         .WithSharedComponentFilterManaged(texture).WithEntityAccess())
            {
                var trail = trailRef.ValueRO;
                ecb.RemoveComponent<TrailSystemData>(entity);
                ecb.RemoveComponent<TrailTexture>(entity);
                texture.Position.SetPixel(trail.X, trail.Y, Color.clear);
            }
            texture.Position.Apply();
            texture.ColorLife.Apply();
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void AddTrail(Entity e, ref EntityCommandBuffer ecb, ref SystemState state, TempTrail t)
    {
        var data = TrailManager.main.RegisterTrail();
        ecb.AddSharedComponentManaged(e, new TrailTexture{Position = data.Item3, ColorLife = data.Item4});
        ecb.AddComponent(e, new TrailData {});
        ecb.AddComponent(e, new TrailSystemData { X = data.Item1, Y = data.Item2, ColorLife = t.ColorLife});
    }
}