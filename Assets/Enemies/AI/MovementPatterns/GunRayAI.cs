using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    class GunRayAI : BaseEnemyAuthor
    {
        [Header("Gun Ray Settings")]
        public float spacingRange;
        public float turnSpeed;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new GunRay
            {
                TurnSpeed = turnSpeed,
                SpacingRange = spacingRange * spacingRange,
            });
            base.Bake(baker, entity);
        }
    }
    
    public struct GunRay : IComponentData
    {
        public float TurnSpeed;
        public float SpacingRange;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct GunRayAISystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        private partial struct GunRayAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform, ref EnemyMovement movement, ref GunRay ray)
            {
                var toPlayer = PlayerPosition - transform.Position;
                
                if (Dim != Dimension.Three) toPlayer.y = 0;
                if (math.lengthsq(toPlayer) < ray.SpacingRange) toPlayer *= -1;
                toPlayer = math.normalize(toPlayer);
                
                toPlayer = MathsBurst.RotateVectorTowards(transform.Forward(), toPlayer, ray.TurnSpeed * DeltaTime);
                movement.TargetRoll = 1;
                movement.TargetFaceDir = toPlayer;
                movement.TargetMoveDir = toPlayer;
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new GunRayAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
        }
    }
}