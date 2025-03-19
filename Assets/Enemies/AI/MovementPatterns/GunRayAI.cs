using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
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
        public float moveWeight;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddSharedComponent(entity, new GunRay
            {
                TurnSpeed = turnSpeed,
                SpacingRange = spacingRange * spacingRange,
                MoveWeight = moveWeight,
            });
            base.Bake(baker, entity);
        }
    }
    
    public struct GunRay : ISharedComponentData
    {
        public float TurnSpeed;
        public float SpacingRange;
        public float MoveWeight;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
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
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform, in PhysicsVelocity velocity, ref EnemyMovement movement, in GunRay ray)
            {
                var toPlayer = PlayerPosition - transform.Position;
                
                if (Dim != Dimension.Three) toPlayer.y = 0;
                if (math.lengthsq(toPlayer) < ray.SpacingRange) toPlayer *= -1;
                toPlayer = math.normalize(toPlayer);
                
                toPlayer = MathsBurst.RotateVectorTowards(transform.Forward(), toPlayer, ray.TurnSpeed * DeltaTime);
                movement.TargetUpDir = ComputeCurveNormal(velocity.Linear, toPlayer, transform.Forward());
                movement.TargetFaceDir = toPlayer;
                movement.TargetMoveVel = toPlayer * ray.MoveWeight;
            }
            
            private float3 ComputeCurveNormal(float3 vel, float3 targetVel, float3 forward)
            {
                float3 perp = math.cross(math.cross(vel, targetVel), forward);
                return math.normalize(perp);
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