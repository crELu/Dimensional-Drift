using Latios.Mecanim;
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
        [Header("Spike Ball Settings")]
        public float attackRange;
        public float hideRange;
        public float spacingRange;
        public float hideHealthFactor;
        public float turnSpeed;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new SpikeBall
            {
                TargetSpeed = turnSpeed,
                SpacingRange = spacingRange,
                AttackRange = attackRange * attackRange,
                HideRange = hideRange * hideRange,
                HideHealthFactor = hideHealthFactor,
            });
            health = hideHealthFactor * health;
            base.Bake(baker, entity);
            health /= hideHealthFactor;
        }
    }
    
    public struct SpikeBall : IComponentData
    {
        public float TargetSpeed;
        public float SpacingRange;
        public float HideHealthFactor;
        public float AttackRange;
        public float HideRange;
        public float AnimTime;
        public bool Attacking;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
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
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform, ref EnemyMovement movement, ref SpikeBall ball, 
                MecanimAspect animator, ref DynamicBuffer<EnemyShoot> guns, ref EnemyStats stats)
            {
                var pPos = MathsBurst.DimSwitcher(PlayerPosition, Dim == Dimension.Three);
                var ePos = MathsBurst.DimSwitcher(transform.Position, Dim == Dimension.Three);
                var d = pPos - ePos;
                
                var distToPlayer = math.lengthsq(d);
                if (ball.Attacking)
                {
                    if (ball.AnimTime < 3)
                    {
                        ball.AnimTime += DeltaTime;
                    }
                    else
                    {
                        if (ball.AnimTime < 4)
                        {
                            ActivateGuns(ref guns, true);
                            
                            ball.AnimTime = 5;
                        }
                        if (distToPlayer > ball.AttackRange || distToPlayer < ball.HideRange)
                        {
                            animator.SetBool("attack", false);
                            ball.Attacking = false;
                            ActivateGuns(ref guns, false);
                            stats.Health *= ball.HideHealthFactor;
                        }
                    }
                }
                else
                {
                    if (distToPlayer < ball.AttackRange && distToPlayer > ball.HideRange)
                    {
                        animator.SetBool("attack", true);
                        stats.Health *= 1 / ball.HideHealthFactor;
                        ball.Attacking = true;
                        ball.AnimTime = 0;
                    }
                }
                
                movement.TargetUpDir = float3.zero;
                movement.TargetFaceDir = math.normalize(d);
                
                movement.TargetMoveVel = MathsBurst.ClampMagnitude(pPos - math.normalize(d) * ball.SpacingRange - ePos, ball.TargetSpeed);
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
            state.Dependency = new SpikeBallAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
        }
    }
}