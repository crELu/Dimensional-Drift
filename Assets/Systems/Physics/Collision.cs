using Enemies.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSimulationGroup))]
public partial struct CollisionSystem : ISystem
{
    // [BurstCompile]
    private struct CollisionEventJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<PhysicsCollider> ColliderLookup;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        public ComponentLookup<PlayerData> PlayerLookup;
        public ComponentLookup<EnemyHealth> EnemyLookup;
        [ReadOnly] public ComponentLookup<DamagePlayer> EnemyWeaponLookup;
        [ReadOnly] public ComponentLookup<PlayerProjectile> PlayerWeaponLookup;
        [ReadOnly] public ComponentLookup<Obstacle> TerrainLookup;
        
        [ReadOnly] public NativeArray<RigidBody> AllBodies;
        [ReadOnly] public float3 PlayerPosition;
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            Calculate(entityA, entityB, collisionEvent);
            Calculate(entityB, entityA, collisionEvent);
        }
        
        private void Calculate(Entity entityA, Entity entityB, CollisionEvent collisionEvent)
        {
            byte aTags = AllBodies[collisionEvent.BodyIndexA].CustomTags;
            byte bTags = AllBodies[collisionEvent.BodyIndexB].CustomTags;

            var colliderA = ColliderLookup.GetRefRO(entityA).ValueRO.Value.Value;
            
            uint aLayers = colliderA.GetCollisionFilter().BelongsTo;
            uint bLayers = ColliderLookup.GetRefRO(entityB).ValueRO.Value.Value.GetCollisionFilter().BelongsTo;

            if (PlayerLookup.HasComponent(entityA) && EnemyWeaponLookup.HasComponent(entityB))
            {
                if (EnemyWeaponLookup.GetRefRO(entityB).ValueRO.DieOnHit)
                {
                    Ecb.DestroyEntity(0, entityB);
                }
            }
            if (EnemyLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB))
            {
                Ecb.DestroyEntity(0, entityB);
            }
            if (TerrainLookup.HasComponent(entityA) && EnemyWeaponLookup.HasComponent(entityB))
            {
                Ecb.DestroyEntity(0, entityB);
            }
            if (TerrainLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB))
            {
                Ecb.DestroyEntity(0, entityB);
            }
            // if (Compare(aLayers, 0) && Compare(bLayers, 10))
            // {
            //     Ecb.DestroyEntity(0, entityA);
            // }
            // if (Compare(aLayers, 1) && Compare(bLayers, 0))
            // {
            //     
            // }
            // if (Compare(aLayers, 2) && Compare(bLayers, 0))
            // {
            //
            // }
        }
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        
        state.Dependency = new CollisionEventJob
        {
            ColliderLookup = SystemAPI.GetComponentLookup<PhysicsCollider>(true),
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            PlayerLookup = SystemAPI.GetComponentLookup<PlayerData>(),
            EnemyWeaponLookup = SystemAPI.GetComponentLookup<DamagePlayer>(true),
            EnemyLookup = SystemAPI.GetComponentLookup<EnemyHealth>(),
            PlayerWeaponLookup = SystemAPI.GetComponentLookup<PlayerProjectile>(true),
            TerrainLookup = SystemAPI.GetComponentLookup<Obstacle>(true),
            AllBodies = physicsWorld.CollisionWorld.Bodies,
            Ecb = GetEntityCommandBuffer(ref state),
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        state.Dependency.Complete();

    }

    private static bool Compare(uint tags, int index)
    {
        return (tags & (1 << index)) > 0;
    }
    
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PhysicsWorldSingleton>();
        state.RequireForUpdate<SimulationSingleton>();
    }

    public void OnDestroy(ref SystemState state) { }
    
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
// [UpdateBefore(typeof(PhysicsSimulationGroup))] // We are updating before `PhysicsSimulationGroup` - this means that we will get the events of the previous frame
// public partial struct GetNumCollisionEventsSystem : ISystem
// {
//     [BurstCompile]
//     public partial struct CountNumCollisionEvents : ICollisionEventsJob
//     {
//         public NativeReference<int> NumCollisionEvents;
//         public void Execute(CollisionEvent collisionEvent)
//         {
//             NumCollisionEvents.Value++;
//         }
//     }
//
//     [BurstCompile]
//     public void OnUpdate(ref SystemState state)
//     {
//         NativeReference<int> numCollisionEvents = new NativeReference<int>(0, Allocator.TempJob);
//         
//         state.Dependency = new CountNumCollisionEvents
//         {
//             NumCollisionEvents = numCollisionEvents
//         }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
//         state.Dependency.Complete();
//
//         numCollisionEvents.Dispose();
//     }
// }