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
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new Lifetime
        {
            Time = lifetime
        });
        base.Bake(baker, entity);
    }
}

public struct Lifetime : IComponentData
{
    public float Time;
}

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
        
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, ref Lifetime t)
        {
            t.Time -= DeltaTime;
            if (t.Time <= 0)
            {
                Ecb.DestroyEntity(chunkIndex, e);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
        new LifetimeJob {Ecb = ecb, DeltaTime = SystemAPI.Time.DeltaTime}.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}
