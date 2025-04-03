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
        public float3 PlayerPos;
        public float DeltaTime;
        
        // Use a thread-safe NativeQueue with a ParallelWriter to store matching enemy entities.
        public NativeQueue<float2>.ParallelWriter enemyQueue;
        public NativeQueue<float4>.ParallelWriter enemyOverlayQueue;
        public Matrix4x4 cameraMatrix;
        public bool Dim3;
        public int ScanState;
        public float ScanRadius;

        public void Execute([EntityIndexInQuery] int index, Entity entity, in LocalTransform transform, in EnemyStats enemy, ref ScannableEnemy scan)
        {
            float2 pos = transform.Position.xz - PlayerPos.xz;
            
            if (pos.x >= -MaxXZ.x && pos.x <= MaxXZ.x &&
                pos.y >= -MaxXZ.y && pos.y <= MaxXZ.y)
            {
                enemyQueue.Enqueue(pos/MaxXZ);
            }
            
            float4 clipPos = math.mul(cameraMatrix, new float4(transform.Position, 1f));
            float3 ndcPos = clipPos.xyz / clipPos.w;
            float range = math.distance(PlayerPos, transform.Position);
            if (ScanState == 0) return;
            if (ScanState == 1)
            {
                if (scan.ScanSize > 0 || range < ScanRadius)
                {
                    scan.ScanSize = math.min(scan.ScanSize + DeltaTime * 2, 1);
                }
            } else if (ScanState == -1)
            {
                if (scan.ScanSize > 0)
                {
                    scan.ScanSize = math.max(scan.ScanSize - DeltaTime * 2, 0);
                }
            }
            if (clipPos.z > 0 || !Dim3)
            {
                var scalefactor = Dim3? math.clamp(1 / range, 0, 1) : .02f;
                enemyOverlayQueue.Enqueue(new float4(ndcPos.x, ndcPos.y, enemy.Size * scalefactor, scan.ScanSize));
            }
        }
    }
    
    public partial struct EnemyFilterSystem : ISystem
    {
        // A persistent queue that will be used each update.
        private NativeQueue<float2> enemyQueue;
        private NativeQueue<float4> enemyOverlayQueue;

        public void OnCreate(ref SystemState state)
        {
            enemyQueue = new NativeQueue<float2>(Allocator.Persistent);
            enemyOverlayQueue = new NativeQueue<float4>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (enemyQueue.IsCreated) enemyQueue.Dispose();
            if (enemyOverlayQueue.IsCreated) enemyOverlayQueue.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            // Clear the queue for this frame.
            enemyQueue.Clear();
            enemyOverlayQueue.Clear();
            
            float3 playerPos = PlayerManager.Position;
            float2 maxXZ = new float2(500) + playerPos.xz;

            var worldToScreenMatrix = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix;
            
            var job = new EnemyFilterJob
            {
                MaxXZ = maxXZ,
                PlayerPos = playerPos,
                enemyQueue = enemyQueue.AsParallelWriter(),
                enemyOverlayQueue = enemyOverlayQueue.AsParallelWriter(),
                cameraMatrix = worldToScreenMatrix,
                ScanRadius = PlayerManager.main.ScanRadius,
                ScanState = PlayerManager.main.ScanState,
                DeltaTime = SystemAPI.Time.DeltaTime,
                Dim3 = DimensionManager.burstDim.Data == Dimension.Three
            };

            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();

            NativeArray<float2> enemyArray = enemyQueue.ToArray(Allocator.Temp);
            NativeArray<float4> enemyOverlayArray = enemyOverlayQueue.ToArray(Allocator.Temp);
            
            if (PlayerManager.main)
            {
                if (PlayerManager.main.MinimapPos != null) PlayerManager.main.MinimapPos.Dispose();
                if (PlayerManager.main.OverlayPos != null) PlayerManager.main.OverlayPos.Dispose();
            }

            if (enemyArray.Length > 0)
            {
                GraphicsBuffer x = new GraphicsBuffer(GraphicsBuffer.Target.Raw, enemyArray.Length, 8);
                x.SetData(enemyArray);
                PlayerManager.main.minimap.SetGraphicsBuffer("Positions", x);
                PlayerManager.main.MinimapPos = x;
            }

            if (enemyOverlayArray.Length > 0)
            {
                GraphicsBuffer y = new GraphicsBuffer(GraphicsBuffer.Target.Raw, enemyOverlayArray.Length, 16);
                y.SetData(enemyOverlayArray);
                PlayerManager.main.overlay.SetGraphicsBuffer("Positions", y);
                PlayerManager.main.OverlayPos = y;
            }

            // Clean up the temporary array.
            enemyArray.Dispose();
            enemyOverlayArray.Dispose();
        }
    }
}