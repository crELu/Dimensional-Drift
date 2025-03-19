using Latios.Mecanim;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    class SwarmerAI : BaseEnemyAuthor
    {
        [Header("Swarmer Settings")]
        public float spacingRange;
        public float strafeWeight;
        public float maxTargetSpeed, maxFarSpeed;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddSharedComponent(entity, new Swarmer
            {
                SpacingRange = spacingRange,
                StrafeWeight = strafeWeight,
                MaxTargetSpeed = maxTargetSpeed,
                MaxFarSpeed = maxFarSpeed
            });
            base.Bake(baker, entity);
        }
    }
    
    public struct Swarmer : ISharedComponentData
    {
        public float StrafeWeight;
        public float SpacingRange;
        public float MaxTargetSpeed;
        public float MaxFarSpeed;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct SwarmerAISystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        private partial struct SwarmerAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform, ref EnemyMovement movement,
                in Swarmer fish)
            {
                var pPos = MathsBurst.DimSwitcher(PlayerPosition, Dim == Dimension.Three);
                var ePos = MathsBurst.DimSwitcher(transform.Position, Dim == Dimension.Three);
                
                var toPlayer = pPos - ePos;
                
                var idealPos = pPos - math.normalize(toPlayer) * fish.SpacingRange;

                var idealVel = idealPos - ePos +
                               math.cross(math.normalize(toPlayer), math.up()) * fish.StrafeWeight;
                movement.TargetUpDir = math.up();
                movement.TargetFaceDir = math.normalize(toPlayer);
                idealVel = MathsBurst.ClampMagnitude(idealVel,
                    math.lengthsq(toPlayer) > 150 * 150 ? fish.MaxFarSpeed : fish.MaxTargetSpeed);
                movement.TargetMoveVel = idealVel;
            }

            private void ActivateGuns(ref DynamicBuffer<EnemyShoot> guns, bool active)
            {
                for (int i = 0; i < guns.Length; i++)
                {
                    guns.ElementAt(i).Active = active;
                }
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new SwarmerAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
        }
    }
}