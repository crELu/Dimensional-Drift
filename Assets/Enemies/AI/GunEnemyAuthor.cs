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
using UnityEngine.Serialization;
using Random = Unity.Mathematics.Random;

class GunEnemyAuthor : BaseEnemyAuthor
{
    public GameObject bullet;
    public float fireRate, fireSpeed;
    public Vector2 spread;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new GunEnemy
        {
            AttackCd = fireRate,
            Speed = fireSpeed,
            Bullet = baker.ToEntity(bullet),
            Spread = spread
        });
        base.Bake(baker, entity);
    }
}

public struct GunEnemy : IComponentData
{
    public float AttackCd, A, Speed;
    public float2 Spread;
    public Entity Bullet;
}

// [BurstCompile]
public partial struct GunEnemyAI : ISystem
{
    private ComponentLookup<LocalTransform> _localTransformLookup;
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
    }

    public void OnDestroy(ref SystemState state) { }
    
    // [BurstCompile]
    private partial struct GunEnemyAIJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter Ecb;
        public float DeltaTime;
        public float3 PlayerPosition;
        [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
    
        private void Execute([ChunkIndexInQuery] int chunkIndex, Entity e, ref EnemyHealth enemy, ref GunEnemy g)
        {
            g.A += DeltaTime;
            
            if (g.A > g.AttackCd)
            {
                g.A -= g.AttackCd;
                var t = LocalTransformLookup[e];
                float3 position = t.Position;
                float3 d = math.normalize(PlayerPosition - position);
                float3 direction = math.mul(MathsBurst.GetRandomRotationWithinCone(ref enemy.RandomSeed, g.Spread.x, g.Spread.y), d);
                
                Entity newEntity = Ecb.Instantiate(chunkIndex, g.Bullet);
                var originalTransform = LocalTransformLookup[g.Bullet];

                Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPositionRotationScale(
                    position,
                    originalTransform.Rotation,
                    originalTransform.Scale
                ));
                var v = new PhysicsVelocity { Linear = direction * g.Speed };
                Ecb.AddComponent(chunkIndex, newEntity, v);
            }
        }
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        _localTransformLookup.Update(ref state);
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
        new GunEnemyAIJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb,
            LocalTransformLookup = _localTransformLookup,
            PlayerPosition = PlayerManager.main.position
        }.ScheduleParallel(); 
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

