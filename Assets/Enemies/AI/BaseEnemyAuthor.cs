using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Random = Unity.Mathematics.Random;

class BaseEnemyAuthor : BaseAuthor
{
    public int health = 10;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new EnemyHealth
        {
            Health = health,
            MaxHealth = health,
            RandomSeed = new Random((uint)UnityEngine.Random.Range(0, 50000))
        });
    }
}

public struct EnemyHealth : IComponentData
{
    public float Health, MaxHealth;
    public Random RandomSeed;
}

// [BurstCompile]
public partial struct BaseEnemyAI : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnDestroy(ref SystemState state) { }
    
    // [BurstCompile]
    public partial struct BaseEnemyAIJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, LocalTransform t, EnemyHealth b)
        {
            if (b.Health <= 0)
            {
                Ecb.DestroyEntity(chunkIndex, e);
            }
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
        new BaseEnemyAIJob {Ecb = ecb}.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}
