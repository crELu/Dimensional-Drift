using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    class SpikeBallAI : BaseEnemyAuthor
    {
        [Header("Gun Ray Settings")]
        public float spacingRange;
        public float turnSpeed;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new SpikeBall
            {
                TurnSpeed = turnSpeed,
                SpacingRange = spacingRange * spacingRange,
            });
            base.Bake(baker, entity);
        }
    }
    
    public struct SpikeBall : IComponentData
    {
        public float TurnSpeed;
        public float SpacingRange;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct SpikeBallAISystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        private partial struct SpikeBallAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform, ref EnemyMovement movement, ref SpikeBall ball)
            {
                var toPlayer = PlayerPosition - transform.Position;
                
                if (Dim != Dimension.Three) toPlayer.y = 0;
                
                movement.TargetRoll = 0;
                movement.TargetFaceDir = math.normalize(toPlayer);
                
                if (math.lengthsq(toPlayer) < ball.SpacingRange) toPlayer *= -1;
                movement.TargetMoveDir = math.normalize(toPlayer);
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SpikeBallAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
        }
    }
}