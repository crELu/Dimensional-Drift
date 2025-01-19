using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine.Serialization;

class GunEnemyAuthoring : BaseEnemyAuthoring
{
    public GameObject Prefab;
    public float FireRate;
    public GunEnemyBaseInfo enemyAIData;
}

class GunEnemyBaker : Baker<GunEnemyAuthoring>
{
    public override void Bake(GunEnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        
        AddComponent(entity, new EnemyHealth
        {
            Health = 10,
            MaxHealth = 10
        });
        
        switch (authoring.enemyAIData)
        {
            case GunEnemyAInfo _:
                AddComponent(entity, new GunEnemyData
                {
                    AttackCd = authoring.FireRate
                });
                break;

            case GunEnemyBInfo _:
                AddComponent(entity, new Gun2EnemyData
                {
                    AttackCd = authoring.FireRate
                });
                break;
        }
    }
}

public struct GunEnemyData : IComponentData
{
    public float AttackCd, A;
    public Entity Bullet;
}

public struct Gun2EnemyData : IComponentData
{
    public float AttackCd, A;
    public Entity Bullet;
}

[BurstCompile]
public partial struct GunEnemyAI : ISystem
{
    public void OnCreate(ref SystemState state) { }

    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    public partial struct GunEnemyAIJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;
    
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, LocalTransform t, EnemyHealth b, GunEnemyData g, PhysicsVelocity p)
        {
            if (b.Health < 1)
            {
                Ecb.DestroyEntity(chunkIndex, e);
            }
        }
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        new GunEnemyAIJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb
        }.ScheduleParallel();
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

