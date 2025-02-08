using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Random = Unity.Mathematics.Random;

namespace Enemies.AI
{
    class BaseEnemyAuthor : BaseAuthor
    {
        public int health = 10;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new EnemyHealth
            {
                Health = health,
                MaxHealth = health,
                RandomSeed = new Random((uint)UnityEngine.Random.Range(0, 50000))
            });
            baker.AddComponent(entity, new EnemyMovePattern
            {
                CurrentMovePatternType = MovePatternType.Wander
            });
            baker.AddComponent(entity, new PhysicsVelocity
            {
                Linear = float3.zero,
                Angular = float3.zero
            });
            baker.AddComponent(entity, new PhysicsMass
            {

                Transform = RigidTransform.identity,
                InverseInertia = float3.zero,
                InverseMass = 0f,
                AngularExpansionFactor = 0f,
                CenterOfMass = default,
                InertiaOrientation = default
            });
        }
    }

    public struct EnemyHealth : IComponentData // rename to enemy base stats
    {
        public int Health, MaxHealth;
        public Random RandomSeed;
        Vector3 targetPosition;  
    }

    public struct EnemyMovePattern : IComponentData
    {
        public MovePatternType CurrentMovePatternType; // move to enemy base stats
    }


// [BurstCompile]
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
        
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity enemy, EnemyMovePattern enemyMovePattern, EnemyHealth enemyHealth)
            {
                if (enemyHealth.Health <= 0)
                {
                    Ecb.DestroyEntity(chunkIndex, enemy);
                }
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            new BaseEnemyAIJob {Ecb = ecb}.ScheduleParallel();
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}