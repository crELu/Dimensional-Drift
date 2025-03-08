using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.Systems
{
    public class SpawnOnDeathAuthoring : BaseAuthor
    {
        public GameObject prefab;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            base.Bake(baker, entity);
        }
    }
    
    public struct TempSpawnOnDeath : IComponentData
    {
        public Entity Prefab;
    }

    public struct SpawnOnDeath : ICleanupComponentData
    {
        public Entity Prefab;
        public float3 Position;
        public quaternion Rotation;
    }
    
    public partial struct TempToRealSpawnSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            // We'll use a simple foreach loop over entities with TempSpawnOnDeath.
            var entityManager = state.EntityManager;
            // Query for entities that have a TempSpawnOnDeath component.
            foreach (var (temp, entity) in SystemAPI.Query<RefRO<TempSpawnOnDeath>>().WithEntityAccess())
            {
                entityManager.AddComponentData(entity, new SpawnOnDeath { Prefab = temp.ValueRO.Prefab });
                entityManager.RemoveComponent<TempSpawnOnDeath>(entity);
            }
        }

        public void OnDestroy(ref SystemState state)
        {
        }
    }
    
    public partial struct SpawnOnDeathSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            // Create an EntityCommandBuffer to record structural changes.
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            // Query for entities that have a SpawnOnDeath component.
            foreach (var (spawn, entity) in SystemAPI.Query<RefRO<SpawnOnDeath>>().WithEntityAccess().WithNone<LocalTransform>())
            {
                ecb.Instantiate(spawn.ValueRO.Prefab);
                ecb.RemoveComponent<SpawnOnDeath>(entity);
            }
            // Apply the recorded changes.
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
            // Clean up if necessary.
        }
    }
}
