using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public class VFXAuthor : BaseAuthor
{
    [ColorUsage(false, true)] public Color color = Color.magenta;
    public float lifetime = 1;
    public Vector4 scale = Vector3.one;
    public string vfxType;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        Color c = color;
        c.a = lifetime;
        baker.AddComponent(entity, new TempTrail
        {
            ColorLife = c,
            Name = vfxType,
            Scale = new Color(scale.x, scale.y, scale.z, scale.w),
        });
    }
}

public struct TempTrail : IComponentData
{
    public FixedString64Bytes Name;
    public Color Scale;
    public Color ColorLife;
}

public struct TrailTexture : ICleanupSharedComponentData, IEquatable<TrailTexture>
{
    public Texture2D Position;
    public Texture2D ColorLife;
    public Texture2D Size;
    public int Id;
    public FixedString64Bytes Name;
    
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
    public Color Scale;
}

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct TrailUpdateSystem : ISystem
{
    private ComponentLookup<TrailData> _trailLookup;
    private ComponentLookup<LocalTransform> _localTransformLookup;
    private ComponentLookup<Parent> _parentLookup;
    private ComponentLookup<PostTransformMatrix> _postTransformLookup;
    
    public void OnCreate(ref SystemState state)
    {
        _trailLookup = state.GetComponentLookup<TrailData>(isReadOnly: true);
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        _parentLookup = state.GetComponentLookup<Parent>(isReadOnly: true);
        _postTransformLookup = state.GetComponentLookup<PostTransformMatrix>(isReadOnly: true);
    }

    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    private partial struct VFXUpdateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [NativeDisableParallelForRestriction] public NativeArray<Color> Position, ColorLife, Size;
            [ReadOnly] public ComponentLookup<TrailData> TrailAlive;
            public int Width;
            [NativeDisableParallelForRestriction] public NativeReference<int> NumDeletions;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformLookup;
    
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, TrailSystemData trail)
            {
                var i = trail.Y * Width + trail.X;
                if (TrailAlive.HasComponent(entity))
                {
                    TransformHelpers.ComputeWorldTransformMatrix(entity, out float4x4 worldMatrix, ref LocalTransformLookup, ref ParentLookup, ref PostTransformLookup);
                    float3 pos = worldMatrix.c3.xyz;
                    Position[i] = new Color(pos.x, pos.y, pos.z, 1);
                    ColorLife[i] = trail.ColorLife;
                    Size[i] = trail.Scale;
                }
                else
                {
                    Ecb.RemoveComponent<TrailSystemData>(chunkIndex, entity);
                    Ecb.RemoveComponent<TrailTexture>(chunkIndex, entity);
                    NumDeletions.Value++;
                    //VFXManager.main.UnregisterParticles(Texture.Name, Texture.Id, 1);
                    Position[i] = Color.clear;
                }
            }
        }
    
    public void OnUpdate(ref SystemState state)
    {
        var uniqueSharedComponents = new List<TrailTexture>();
        state.EntityManager.GetAllUniqueSharedComponentsManaged(uniqueSharedComponents);
        
        var query = SystemAPI.QueryBuilder()
            .WithAll<TrailSystemData>()
            .WithAll<TrailTexture>()
            .Build();
        
        foreach (var texture in uniqueSharedComponents)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            if (texture.Position == null) continue;
            
            query.SetSharedComponentFilterManaged(texture);
            
            _trailLookup.Update(ref state);
            _localTransformLookup.Update(ref state);
            _parentLookup.Update(ref state);
            _postTransformLookup.Update(ref state);
            
            var position = texture.Position.GetRawTextureData<Color>();
            var color = texture.ColorLife.GetRawTextureData<Color>();
            var size = texture.Size.GetRawTextureData<Color>();
            var free = new NativeReference<int>(Allocator.TempJob);
            var job = new VFXUpdateJob
            {
                Ecb = ecb.AsParallelWriter(), Position = position, ColorLife = color, Size = size,
                TrailAlive = _trailLookup, Width = texture.Position.width,
                LocalTransformLookup = _localTransformLookup,
                ParentLookup = _parentLookup,
                PostTransformLookup = _postTransformLookup,
                NumDeletions = free
            };
            job.ScheduleParallel(query, state.Dependency).Complete();
            
            VFXManager.main.UnregisterParticles(texture.Name, texture.Id, free.Value);

            texture.Position.Apply();
            texture.ColorLife.Apply();
            texture.Size.Apply();
            
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

public partial struct TrailSpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state) { }

    public void OnDestroy(ref SystemState state) { }
    
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (tempTrail, entity) in SystemAPI.Query<RefRW<TempTrail>>().WithEntityAccess())
        {
            ecb.RemoveComponent<TempTrail>(entity);
            AddTrail(entity, ref ecb, ref state, tempTrail.ValueRO);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    private void AddTrail(Entity e, ref EntityCommandBuffer ecb, ref SystemState state, TempTrail t)
    {
        var dataTuple = VFXManager.main.RegisterParticle(t.Name);
        if (!dataTuple.HasValue) return;
        var (data, id) = dataTuple.Value;
        
        ecb.AddSharedComponentManaged(e, new TrailTexture{Position = data.Positions, ColorLife = data.ColorLife, Size = data.Size, Id = id, Name = t.Name});
        ecb.AddComponent(e, new TrailData {});
        ecb.AddComponent(e, new TrailSystemData { X = data.X, Y = data.Y, ColorLife = t.ColorLife, Scale = t.Scale});
    }
}