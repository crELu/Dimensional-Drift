using Enemies.AI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Player
{
    [BurstCompile]
    public partial struct EnemyFilterJob : IJobEntity
    {
        // Define the bounds for x and z.
        // Here, minXZ.x is the minimum x and minXZ.y is the minimum z.
        public float2 MaxXZ;
        public float2 Offset;

        // Use a thread-safe NativeQueue with a ParallelWriter to store matching enemy entities.
        public NativeQueue<float2>.ParallelWriter enemyQueue;

        public void Execute(Entity entity, in LocalTransform transform, in EnemyStats enemy)
        {
            float2 pos = transform.Position.xz - Offset;
            // Check if the enemy's x and z fall within the specified range.
            if (pos.x >= -MaxXZ.x && pos.x <= MaxXZ.x &&
                pos.y >= -MaxXZ.y && pos.y <= MaxXZ.y)
            {
                enemyQueue.Enqueue(pos/MaxXZ);
            }
        }
    }
    
    public partial struct EnemyFilterSystem : ISystem
    {
        // A persistent queue that will be used each update.
        private NativeQueue<float2> enemyQueue;

        public void OnCreate(ref SystemState state)
        {
            enemyQueue = new NativeQueue<float2>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (enemyQueue.IsCreated)
                enemyQueue.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Clear the queue for this frame.
            enemyQueue.Clear();

            // Define the range in the XZ plane.
            float3 playerPos = PlayerManager.Position;
            float2 maxXZ = new float2(500) + playerPos.xz;

            // Schedule the job.
            var job = new EnemyFilterJob
            {
                MaxXZ = maxXZ,
                Offset = playerPos.xz,
                enemyQueue = enemyQueue.AsParallelWriter()
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            NativeArray<float2> enemyArray = enemyQueue.ToArray(Allocator.Temp);
            
            if (PlayerManager.main.Px != null)
            {
                PlayerManager.main.Px.Dispose();
            }
            
            if (enemyArray.Length > 0)
            {
                GraphicsBuffer x = new GraphicsBuffer(GraphicsBuffer.Target.Raw, enemyArray.Length * 2, 4);
                x.SetData(enemyArray);
                PlayerManager.main.minimap.SetGraphicsBuffer("Positions", x);
                PlayerManager.main.Px = x;
            }

            // Clean up the temporary array.
            enemyArray.Dispose();
        }
    }
}