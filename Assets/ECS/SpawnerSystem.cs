using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

[BurstCompile]
public partial struct OptimizedSpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        // Creates a new instance of the job, assigns the necessary data, and schedules the job in parallel.
        new ProcessSpawnerJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Ecb = ecb
        }.ScheduleParallel();
    }
    
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

[BurstCompile]
public partial struct ProcessSpawnerJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter Ecb;
    public float DeltaTime;
    
    // IJobEntity generates a component data query based on the parameters of its `Execute` method.
    // This example queries for all Spawner components and uses `ref` to specify that the operation
    // requires read and write access. Unity processes `Execute` for each entity that matches the
    // component data query.
    private void Execute([ChunkIndexInQuery] int chunkIndex, SpawnerAspect spawner)
    {
        spawner.NextSpawnTime += DeltaTime;

        spawner.Transform = spawner.Transform.RotateY(DeltaTime * spawner.Data.RotationSpeed);
        if (spawner.NextSpawnTime > spawner.Data.SpawnRate)
        {
            spawner.NextSpawnTime = 0;
            float angleStep = math.radians((spawner.Data.Arc.y - spawner.Data.Arc.x) / spawner.Data.Count);
            for (int i = 0; i < spawner.Data.Count; i++)
            {
                float angle = math.radians(spawner.Data.Arc.x) + angleStep * i;
                
                
                float3 position =  math.mul(math.mul(spawner.WTransform.Rotation, quaternion.Euler(0, angle, 0)), new float3(0, 0, 1));
                float3 direction = math.normalize(position);

                Entity newEntity = Ecb.Instantiate(chunkIndex, spawner.Prefab);
                Ecb.SetComponent(chunkIndex, newEntity, LocalTransform.FromPosition(spawner.WTransform.Position + position));
                //Ecb.SetComponent(chunkIndex, newEntity, new RigidBody());
                Ecb.AddComponent(chunkIndex, newEntity, new PhysicsVelocity());
                Ecb.AddComponent(chunkIndex, newEntity, new ProjectileData {Velocity = direction * spawner.ProjData.Speed, Lifetime = spawner.ProjData.Lifetime});
            }
        }
    }
}