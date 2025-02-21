using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Enemies
{
    
    public struct WaveSingleton : IComponentData
    {
        public int Wave;
        public float WaveTimer;
    }

    // [BurstCompile]
    public partial struct WaveManager : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        
        public void OnCreate(ref SystemState state)
        {
            Entity entity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(entity, new WaveSingleton {});
        }

        public void OnDestroy(ref SystemState state) { }
    
        // [BurstCompile]
        private partial struct WaveJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public Vector3 PlayerPosition;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
    
            private void Execute([ChunkIndexInQuery] int chunkIndex, 
                Entity enemy)
            {
                
            }
        }

        // [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}