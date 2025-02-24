using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    class MeleeRayAI : BaseEnemyAuthor
    {
        [Header("Melee Ray Settings")] 
        public float turnSpeed;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new MeleeRay
            {
                TurnSpeed = turnSpeed,
            });
            base.Bake(baker, entity);
        }
    }
    
    public struct MeleeRay : IComponentData
    {
        public float TurnSpeed;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct MeleeRayAISystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
        
        // [BurstCompile]
        private partial struct MeleeRayAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform, ref EnemyMovement movement, ref MeleeRay ray)
            {
                var toPlayer = PlayerPosition - transform.Position;
                if (Dim != Dimension.Three) toPlayer.y = 0;
                
                toPlayer = math.normalize(toPlayer);
                toPlayer = MathsBurst.RotateVectorTowards(transform.Forward(), toPlayer, ray.TurnSpeed * DeltaTime);
                movement.TargetRoll = 1;
                movement.TargetFaceDir = toPlayer;
                movement.TargetMoveDir = toPlayer;
            }
        }
        
        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new MeleeRayAIJob
            {
                PlayerPosition = PlayerManager.Position,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data
            }.ScheduleParallel(state.Dependency); 
        }
    }
}