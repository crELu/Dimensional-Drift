using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Serialization;

class DamagePlayerAuthor : BaseAuthor
{
    public int damage;
    public int weight = -1;
    public bool dieOnHit;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new DamagePlayer
        {
            Damage = damage,
            DieOnHit = dieOnHit,
            Mass = weight,
        });
        base.Bake(baker, entity);
    }
}

public struct DamagePlayer : IComponentData
{
    public int Damage;
    public int Mass;
    public bool DieOnHit;
}

[BurstCompile]
[UpdateAfter(typeof(PhysicsSystem))]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct DamagePlayerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    public partial struct DeathJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, DamagePlayer t)
        {
            if (t.Mass == 0)
            {
                Ecb.DestroyEntity(chunkIndex, e);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
        new DeathJob {Ecb = ecb}.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}