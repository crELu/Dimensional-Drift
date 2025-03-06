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
    [BurstCompile]
    private struct CollisionEventJob : ICollisionEventsJob
    {
        public ComponentLookup<PlayerData> PlayerLookup;
        public ComponentLookup<EnemyStats> EnemyLookup;
        public ComponentLookup<PlayerProjectile> PlayerWeaponLookup;
        [ReadOnly] public ComponentLookup<DamagePlayer> EnemyWeaponLookup;
        [ReadOnly] public ComponentLookup<Obstacle> TerrainLookup;
        [ReadOnly] public ComponentLookup<LaserTag> LaserTagLookup;
        
        public EntityCommandBuffer.ParallelWriter Ecb;
        
        [BurstCompile]
        public void Execute(CollisionEvent collisionEvent)
        {
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            Calculate(entityA, entityB);
            Calculate(entityB, entityA);
        }
        
        [BurstCompile]
        private void Calculate(Entity entityA, Entity entityB)
        {
            //Debug.Log($"OK! {EnemyLookup.HasComponent(entityA)} {PlayerWeaponLookup.HasComponent(entityB)}");
            if (PlayerLookup.HasComponent(entityA) && EnemyWeaponLookup.HasComponent(entityB)) // Enemy Hit Player
            {
                if (EnemyWeaponLookup.GetRefRO(entityB).ValueRO.DieOnHit)
                {
                    Ecb.DestroyEntity(0, entityB);
                }
            }
            if (EnemyLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB)) // Player Hit Enemy
            {
                PlayerProjectile playerProj = PlayerWeaponLookup.GetRefRW(entityB).ValueRW;
                EnemyStats enemy = EnemyLookup.GetRefRW(entityA).ValueRW;
                if (LaserTagLookup.HasComponent(entityB))
                {
                    enemy.Health -= playerProj.Stats.damage * Time.deltaTime;
                }
                else
                {
                    enemy.Health -= playerProj.Stats.damage;
                    playerProj.Health -= (int)math.ceil(10000f / (1+playerProj.Stats.pierce));
                }

                
                EnemyLookup.GetRefRW(entityA).ValueRW = enemy;
                PlayerWeaponLookup.GetRefRW(entityB).ValueRW = playerProj;
                
                if (playerProj.Health == 0)
                {
                    Ecb.DestroyEntity(0, entityB);
                }
            }
            if (TerrainLookup.HasComponent(entityA) && EnemyWeaponLookup.HasComponent(entityB))
            {
                Ecb.DestroyEntity(0, entityB);
            }
            if (TerrainLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB))
            {
                Ecb.DestroyEntity(0, entityB);
            }
        }
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        
        // state.Dependency = new CollisionEventJob
        // {
        //     PlayerLookup = SystemAPI.GetComponentLookup<PlayerData>(),
        //     EnemyWeaponLookup = SystemAPI.GetComponentLookup<DamagePlayer>(true),
        //     EnemyLookup = SystemAPI.GetComponentLookup<EnemyStats>(),
        //     PlayerWeaponLookup = SystemAPI.GetComponentLookup<PlayerProjectile>(),
        //     TerrainLookup = SystemAPI.GetComponentLookup<Obstacle>(true),
        //     Ecb = GetEntityCommandBuffer(ref state),
        // }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        // state.Dependency.Complete();

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