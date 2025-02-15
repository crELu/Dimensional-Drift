using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using LocalTransform = Unity.Transforms.LocalTransform;
using Random = Unity.Mathematics.Random;

namespace Enemies.AI
{
    class BaseEnemyAuthor : BaseAuthor
    {
        [Header("Enemy Settings")]
        public int health = 10;
        public float baseMovementSpeed;
        public float baseRotationSpeed;
        public float baseAcceleration;
        public float baseDrag;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new EnemyStats
            {
                Health = health,
                MaxHealth = health,
                RandomSeed = new Random((uint)UnityEngine.Random.Range(0, 50000)),
                TargetPosition = float3.zero, // transform.position,
                BaseMovementSpeed = baseMovementSpeed,
                BaseRotationSpeed = baseRotationSpeed,
                MovementSpeedModifiers = 0,
                BaseAcceleration = baseAcceleration,
                BaseDrag = baseDrag
            });
        }
    }
    
    public struct EnemyStats : IComponentData 
    {
        public float Health, MaxHealth;
        public Random RandomSeed;
        public float3 TargetPosition;
        public float BaseMovementSpeed;
        public float BaseRotationSpeed;
        public float MovementSpeedModifiers;
        public float BaseAcceleration;
        public float BaseDrag;
        //
        // public EnemyStats(float baseMovementSpeed = 30, float baseRotationSpeed = 30, float baseAcceleration = 1, float baseDrag = 1)
        // {
        //     BaseMovementSpeed = baseMovementSpeed;
        //     BaseRotationSpeed = baseRotationSpeed;
        //     MovementSpeedModifiers = 0;
        //     BaseAcceleration = baseAcceleration;
        //     BaseDrag = baseDrag;
        //     Health = 0;
        //     MaxHealth = 0;
        //     RandomSeed = new Random((uint)UnityEngine.Random.Range(0, 50000));
        //     TargetPosition = float3.zero;
        // }
    }


    [BurstCompile]
    public partial struct BaseEnemyAI : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnDestroy(ref SystemState state) { }
    
        // [BurstCompile]
        public partial struct BaseEnemyAIJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            
            private float GetSpeed(EnemyStats enemyStats, PhysicsVelocity physicsVelocity)
            {
                float percentTopSpeed = math.max(0.0001f, Vector3.Magnitude(
                                            physicsVelocity.Linear) /
                                        (enemyStats.BaseMovementSpeed +
                                         enemyStats.MovementSpeedModifiers));
                return enemyStats.BaseAcceleration * (1 - percentTopSpeed) * DeltaTime;
            }

            private Quaternion GetRotation(ref LocalTransform transform, EnemyStats enemyStats, 
                Vector3 direction)
            {
                quaternion currentRotation = transform.Rotation;
                quaternion targetRotation = quaternion.LookRotationSafe(direction, math.up());
                float rotationFactor = math.saturate(enemyStats.BaseRotationSpeed * DeltaTime);
                Quaternion newRotation = math.slerp(currentRotation, targetRotation, rotationFactor);
                return newRotation;
            }

            private void MoveEnemy(ref LocalTransform transform, 
                EnemyStats enemyStats, ref PhysicsVelocity physicsVelocity)
            {   
                float distance = Vector3.Distance(
                    transform.Position, enemyStats.TargetPosition);
                if (distance > Mathf.Epsilon)
                {
                    Vector3 direction = 
                        math.normalize(enemyStats.TargetPosition - transform.Position);
                    Quaternion newRotation =  GetRotation(ref transform, enemyStats, direction);

                    Vector3 enemyDirection = newRotation * Vector3.forward;
                    float speed = GetSpeed(enemyStats, physicsVelocity);
                    physicsVelocity.Linear = enemyDirection * speed;
                }
            }
            private void Execute([ChunkIndexInQuery] int chunkIndex, 
                Entity enemy, EnemyStats enemyStats, ref LocalTransform transform, 
                ref PhysicsVelocity physicsVelocity)
            {
                if (enemyStats.Health <= 0)
                {
                    Ecb.DestroyEntity(chunkIndex, enemy);
                }
                MoveEnemy(ref transform, enemyStats, 
                    ref physicsVelocity);
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            new BaseEnemyAIJob
            {
                Ecb = ecb,
                DeltaTime = SystemAPI.Time.fixedDeltaTime
            }.ScheduleParallel();
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}