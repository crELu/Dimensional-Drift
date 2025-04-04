using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    public struct Yuumi : ICleanupComponentData
    {
        public Entity Attached;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct YuumiAISystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;

        public void OnCreate(ref SystemState state)
        {
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
            state.RequireForUpdate<Yuumi>();
        }

        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        private partial struct YuumiAIJob : IJobEntity
        {
            [NativeDisableParallelForRestriction] public ComponentLookup<LocalTransform> LocalTransform;
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref Yuumi cat, ref PhysicsVelocity velocity)
            {
                var transform = LocalTransform[entity];
                var attachedTransform = LocalTransform[cat.Attached];
                transform.Position = attachedTransform.Position;
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
                LocalTransform.GetRefRW(entity).ValueRW = transform;
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            foreach (var (temp, entity) in SystemAPI.Query<RefRO<Yuumi>>().WithEntityAccess().WithNone<LocalTransform>())
            {
                var enemy = temp.ValueRO.Attached;
                if (SystemAPI.Exists(enemy) && SystemAPI.HasComponent<EnemyCollisionReceiver>(enemy))
                {
                    var enemyStats = state.EntityManager.GetComponentData<EnemyCollisionReceiver>(enemy);
                    enemyStats.Invulnerable = false;
                    ecb.SetComponent(enemy, enemyStats);
                }
                ecb.RemoveComponent<Yuumi>(entity);
            }
            foreach (var (temp, entity) in SystemAPI.Query<RefRO<Yuumi>>().WithEntityAccess())
            {
                var enemy = temp.ValueRO.Attached;
                if (!SystemAPI.Exists(enemy)) ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            
            _localTransformLookup.Update(ref state);
            state.Dependency = new YuumiAIJob
            {
                // PlayerPosition = PlayerManager.burstPos.Data,
                // DeltaTime = SystemAPI.Time.fixedDeltaTime,
                // Dim = DimensionManager.burstDim.Data,
                LocalTransform = _localTransformLookup,
            }.ScheduleParallel(state.Dependency); 
        }
    }
}