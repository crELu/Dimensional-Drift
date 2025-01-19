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
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct CollisionSystem : ISystem
{
    [BurstCompile]
    private struct CollisionEventJob : ICollisionEventsJob
    {
        [ReadOnly] public ComponentLookup<PhysicsCollider> Collider;
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;
        
        [ReadOnly] public NativeArray<RigidBody> AllBodies;
        [ReadOnly] public float3 PlayerPosition;
        public EntityCommandBuffer.ParallelWriter Ecb;
        public NativeReference<bool> HitDetected;
        public NativeReference<float3> HitVelocity;
        public NativeReference<int> HitCount;
        
        public void Execute(CollisionEvent collisionEvent)
        {
            // Retrieve entities involved in the collision
            Entity entityA = collisionEvent.EntityA;
            Entity entityB = collisionEvent.EntityB;

            Calculate(entityA, entityB, collisionEvent);
            Calculate(entityB, entityA, collisionEvent);
        }
        
        private void Calculate(Entity entityA, Entity entityB, CollisionEvent collisionEvent)
        {
            byte aTags = AllBodies[collisionEvent.BodyIndexA].CustomTags;
            byte bTags = AllBodies[collisionEvent.BodyIndexB].CustomTags;

            var colliderA = Collider.GetRefRO(entityA).ValueRO.Value.Value;
            
            uint aLayers = colliderA.GetCollisionFilter().BelongsTo;
            uint bLayers = Collider.GetRefRO(entityB).ValueRO.Value.Value.GetCollisionFilter().BelongsTo;
            
            //Debug.Log($"{aTags} {bTags} {aLayers} {bLayers}");
            if (Compare(aLayers, 0) && Compare(bLayers, 10))
            {
                //Debug.Log($"Collision detected between {entityA} and {entityB}");
                Ecb.DestroyEntity(0, entityA);
            }
            if (Compare(aLayers, 1) && Compare(bLayers, 0))
            {
                HitDetected.Value = true;
                HitVelocity.Value += collisionEvent.Normal;
                HitCount.Value++;
            }
            if (Compare(aLayers, 2) && Compare(bLayers, 0))
            {

            }
        }
    }
    
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        var hitDetected = new NativeReference<bool>(Allocator.TempJob);
        var hitVelocity = new NativeReference<float3>(Allocator.TempJob);
        var hitCount = new NativeReference<int>(Allocator.TempJob);
       
        // Schedule the collision job
        state.Dependency = new CollisionEventJob
        {
            Collider = SystemAPI.GetComponentLookup<PhysicsCollider>(true),
            Transform = SystemAPI.GetComponentLookup<LocalTransform>(true),
            AllBodies = physicsWorld.CollisionWorld.Bodies,
            Ecb = GetEntityCommandBuffer(ref state),
            HitDetected = hitDetected,
            HitVelocity = hitVelocity,
            HitCount = hitCount
        }.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
        
        state.Dependency.Complete();

        hitVelocity.Dispose();
        hitCount.Dispose();
        hitDetected.Dispose();
    }

    private static bool Compare(uint tags, int index)
    {
        return (tags & (1 << index)) > 0;
    }
    
    public void OnCreate(ref SystemState state)
    {
        // Ensure the physics world is available for collision events
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