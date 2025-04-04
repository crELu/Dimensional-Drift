using Latios.Mecanim;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    class SunfishAI : BaseEnemyAuthor
    {
        [Header("Sunfish Settings")]
        public float spacingRange;
        public float attackRange;
        public float strafeWeight;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new Sunfish
            {
                AttackRange = attackRange * attackRange,
                SpacingRange = spacingRange,
                StrafeWeight = strafeWeight,
            });
            base.Bake(baker, entity);
        }
    }
    
    public struct Sunfish : IComponentData
    {
        public float StrafeWeight;
        public float AttackRange;
        public float SpacingRange;
        public float AnimTime;
        public bool Attacking;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct SunfishAISystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        private partial struct SunfishAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;
            private void Execute(in LocalTransform transform, ref EnemyMovement movement,
                ref Sunfish fish, MecanimAspect animator, ref DynamicBuffer<EnemyShoot> guns)
            {
                var pPos = MathsBurst.DimSwitcher(PlayerPosition, Dim == Dimension.Three);
                var ePos = MathsBurst.DimSwitcher(transform.Position, Dim == Dimension.Three);
                
                var toPlayer = pPos - ePos;
                
                var distToPlayer = math.lengthsq(toPlayer);
                
                if (fish.Attacking)
                {
                    if (fish.AnimTime < 2)
                    {
                        fish.AnimTime += DeltaTime;
                    }
                    else
                    {
                        if (fish.AnimTime < 4)
                        {
                            ActivateGuns(ref guns, true);
                            fish.AnimTime = 5;
                        }
                        if (distToPlayer > fish.AttackRange)
                        {
                            animator.SetBool("attack", false);
                            fish.Attacking = false;
                            ActivateGuns(ref guns, false);
                        }
                    }
                }
                else
                {
                    if (distToPlayer < fish.AttackRange)
                    {
                        animator.SetBool("attack", true);
                        fish.Attacking = true;
                        fish.AnimTime = 0;
                    }
                }

                var idealPos = pPos - math.normalize(toPlayer) * fish.SpacingRange;

                var idealVel = idealPos - ePos +
                               math.cross(math.normalize(toPlayer), math.up()) * fish.StrafeWeight;
                movement.TargetUpDir = math.up();
                movement.TargetFaceDir = math.normalize(idealVel);
                
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
            state.Dependency = new SunfishAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data.Position,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
        }
    }
}