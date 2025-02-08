using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace Enemies.AI
{
    class LinearVelocityAuthor : BaseAuthor
    {
        public Vector3 direction = Vector3.right;
        public float speed = 1;
    
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new LinearVelocity
            {
                Direction = direction,
                Speed = speed
            });
            baker.AddComponent(entity, new PhysicsVelocity());
        }
    }

    public struct LinearVelocity : IComponentData
    {
        public float3 Direction;
        public float Speed;
    }

// [BurstCompile]
    public partial struct LinearVelocitySystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
    
        // [BurstCompile]
        private partial struct ProcessLinearVelocityJob : IJobEntity
        {
            private void Execute([ChunkIndexInQuery] int chunkIndex, LinearVelocity l, ref PhysicsVelocity p)
            {
                //p.Linear = l.Direction * l.Speed;
            }
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new ProcessLinearVelocityJob().ScheduleParallel();
        }

    }
}