using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Enemies.AI
{
    class PlayerHomingAuthor : BaseAuthor
    {
        public float homingSpeed = 1;
    
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new PlayerHoming
            {
                HomingSpeed = homingSpeed
            });
        }
    }

    public struct PlayerHoming : IComponentData
    {
        public float HomingSpeed;
    }

// [BurstCompile]
    public partial struct PlayerHomingSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
    
        // [BurstCompile]
        private partial struct ProcessPlayerHomingJob : IJobEntity
        {
            public float3 Target;
            public float DeltaTime;
            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform t, in PlayerHoming h, ref PhysicsVelocity p)
            {
                // float3 directionToTarget = math.normalize(Target - t.Position);
                //
                // float3 adjustedDirection = math.lerp(
                //     math.normalize(p.Linear), 
                //     directionToTarget, 
                //     h.HomingSpeed * DeltaTime
                // );
                //
                // p.Linear = math.normalize(adjustedDirection) * math.length(p.Linear);
                // Calculate the direction to the target
                float3 directionToTarget = math.normalize(Target - t.Position);

                // Get the current velocity direction
                float3 currentDirection = math.normalize(p.Linear);

                // Calculate the rotation required to face the target
                quaternion currentRotation = quaternion.LookRotationSafe(currentDirection, math.up());
                quaternion targetRotation = quaternion.LookRotationSafe(directionToTarget, math.up());

                // Perform spherical interpolation to smoothly rotate towards the target
                quaternion smoothedRotation = math.slerp(currentRotation, targetRotation, h.HomingSpeed * DeltaTime);

                // Extract the new forward direction from the smoothed rotation
                float3 newDirection = math.forward(smoothedRotation);

                // Preserve the original speed
                float speed = math.length(p.Linear);
                p.Linear = newDirection * speed;
            }
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ProcessPlayerHomingJob
            {
                Target = PlayerManager.main.position,
                DeltaTime = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();
        }
    }
}