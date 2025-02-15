using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Enemies.AI
{
    class GunEnemyAuthor : BaseEnemyAuthor
    {
        public GameObject bullet;
        public float fireRate, fireSpeed;
        public Vector2 spread;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new GunEnemyWeaponStats
            {
                AttackCd = fireRate,
                Speed = fireSpeed,
                Bullet = baker.ToEntity(bullet),
                Spread = spread
            });
            base.Bake(baker, entity);
        }
    }

    public struct GunEnemyWeaponStats : IComponentData
    {
        public float AttackCd, AttackDowntime, Speed;
        public float2 Spread;
        public Entity Bullet;
    }

// [BurstCompile]
    public partial struct GunEnemyShootAI : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        }

        public void OnDestroy(ref SystemState state) { }
    
        // [BurstCompile]
        private partial struct GunEnemyShootAIJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public Vector3 PlayerPosition;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
    
            private void Execute([ChunkIndexInQuery] int chunkIndex, 
                Entity enemy, ref EnemyStats enemyStats, ref GunEnemyWeaponStats enemyWeaponStats)
            {
                LocalTransform transform = LocalTransformLookup[enemy];
                DoAttackUpdate(chunkIndex, ref enemyStats, 
                    ref enemyWeaponStats, transform);
            }

            private void DoAttackUpdate(int chunkIndex, 
                ref EnemyStats enemyStats, 
                ref GunEnemyWeaponStats enemyWeaponStats, 
                LocalTransform transform)
            {
                enemyWeaponStats.AttackDowntime += DeltaTime;
                if (enemyWeaponStats.AttackDowntime > enemyWeaponStats.AttackCd)
                {
                    enemyWeaponStats.AttackDowntime -= enemyWeaponStats.AttackCd;
                    Vector3 position = transform.Position;
                    Vector3 d = math.normalize(PlayerPosition - position);
                    Vector3 direction = math.mul(
                        MathsBurst.GetRandomRotationWithinCone(
                            ref enemyStats.RandomSeed, enemyWeaponStats.Spread.x, 
                            enemyWeaponStats.Spread.y), d);
                
                    Entity newEntity = Ecb.Instantiate(chunkIndex, enemyWeaponStats.Bullet);
                    var originalTransform = LocalTransformLookup[enemyWeaponStats.Bullet];

                    Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPositionRotationScale(
                        position,
                        originalTransform.Rotation,
                        originalTransform.Scale
                    ));
                    var velocity = new PhysicsVelocity { Linear = direction * enemyWeaponStats.Speed };
                    Ecb.AddComponent(chunkIndex, newEntity, velocity);
                }                
            }
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localTransformLookup.Update(ref state);
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            new GunEnemyShootAIJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                Ecb = ecb,
                LocalTransformLookup = _localTransformLookup,
                PlayerPosition = PlayerManager.main.position,
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