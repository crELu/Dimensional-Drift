using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

class LifetimeAuthor : BaseAuthor
{
    public int lifetime;
    public float dimensionalDamage;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new Lifetime
        {
            Time = lifetime
        });
        if (dimensionalDamage != 0)
        {
            baker.AddComponent(entity, new LifetimeModifier
            {
                DimensionalDamage = dimensionalDamage
            });
        }
        base.Bake(baker, entity);
    }
}

public struct Lifetime : IComponentData
{
    public float Time;
}

public struct LifetimeModifier : IComponentData
{
    public float DimensionalDamage;
}

public readonly partial struct LifetimeAspect : IAspect
{
    private readonly RefRW<Lifetime> _car;
    [Optional]
    private readonly RefRO<LifetimeModifier> _lifetimeModifier;
    public bool HasModifier => _lifetimeModifier.IsValid;
    public LifetimeModifier Modifier => _lifetimeModifier.ValueRO;
    public float Lifetime
    {
        get => _car.ValueRW.Time;
        set => _car.ValueRW.Time = value;
    }
}

[BurstCompile]
public partial struct LifetimeSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    public partial struct LifetimeJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;
        public bool Dim3;
        
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, LifetimeAspect t)
        {
            t.Lifetime -= DeltaTime * (!Dim3 && t.HasModifier? 1 + t.Modifier.DimensionalDamage : 1 );
            if (t.Lifetime <= 0)
            {
                Ecb.DestroyEntity(chunkIndex, e);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
        new LifetimeJob {Ecb = ecb, DeltaTime = SystemAPI.Time.DeltaTime, Dim3 = DimensionManager.burstDim.Data == Dimension.Three}.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}