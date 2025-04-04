
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public class PlayerProjectileHomingAuthor : BaseAuthor
{
    public float radius;
    public float turnSpeed;
    public float offset;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerProjectileHoming {Radius = radius, TurnSpeed = turnSpeed, Offset = offset});
    }
}

public struct PlayerProjectileHoming : IComponentData
{
    public float Radius;
    public float TurnSpeed;
    public float Offset;
}

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[BurstCompile]
public partial struct PlayerProjectileHomingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }
    
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
    }
    
    [BurstCompile]
    partial struct HomingJob : IJobEntity
    {
        [ReadOnly] public PhysicsSystemState PhysicsState;
        public bool Dim3;
        public float DeltaTime;
        public void Execute(in PlayerProjectileHoming projectile, ref LocalTransform transform, ref PhysicsVelocity velocity)
        {
            var turnSpeed = projectile.TurnSpeed;
            var nearestEnemyPos = float3.zero;
            var minDistSq = float.MaxValue;
            var foundTarget = false;
            var pos = transform.Position + projectile.Offset * projectile.Radius * math.normalize(velocity.Linear);
            PhysicsState.GetInRadius(pos, projectile.Radius, PhysicsState.EnemyLayer, out BodiesInRadius enemyInRadius);
            foreach (FindObjectsResult result in enemyInRadius.enumerator)
            {
                float distSq = math.distancesq(result.transform.position, transform.Position);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    nearestEnemyPos = result.transform.position;
                    foundTarget = true;
                }
            }
            PhysicsState.GetInRadius(pos, projectile.Radius, PhysicsState.EnemyGhostLayer, out BodiesInRadius ghostInRadius);
            foreach (FindObjectsResult result in ghostInRadius.enumerator)
            {
                float distSq = math.distancesq(result.transform.position, transform.Position);
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    nearestEnemyPos = result.transform.position;
                    foundTarget = true;
                }
            }

            if (foundTarget)
            {
                float3 currentDir = math.normalize(transform.Forward());
                float3 targetDir = math.normalizesafe(MathsBurst.DimSwitcher(nearestEnemyPos - transform.Position, Dim3));
                float speed = math.length(velocity.Linear);
                float3 newDir = MathsBurst.RotateVectorTowards(currentDir, targetDir, turnSpeed * DeltaTime);
                transform.Rotation = quaternion.LookRotationSafe(newDir, math.up());
                velocity.Linear = newDir * speed;
            }
        }
    }
    
    [BurstCompile] 
    public void OnUpdate(ref SystemState state)
    {
        var dim3 = DimensionManager.burstDim.Data == Dimension.Three;
        RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
        
        state.Dependency = new HomingJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            Dim3 = dim3,
            PhysicsState = physicsState.ValueRO
        }.ScheduleParallel(state.Dependency); 
        // foreach (var (projectile, transformRef, velocityRef) in SystemAPI.Query<PlayerProjectileHoming, RefRW<LocalTransform>, RefRW<PhysicsVelocity>>())
        // {
        //     var transform = transformRef.ValueRW;
        //     var velocity = velocityRef.ValueRW;
        //     RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
        //     var turnSpeed = projectile.TurnSpeed;
        //     var nearestEnemyPos = float3.zero;
        //     var minDistSq = float.MaxValue;
        //     var foundTarget = false;
        //     
        //     physicsState.ValueRO.GetInRadius(transform.Position, projectile.Radius, physicsState.ValueRO.EnemyLayer, out BodiesInRadius enemyInRadius);
        //     foreach (FindObjectsResult result in enemyInRadius.enumerator)
        //     {
        //         float distSq = math.distancesq(result.transform.position, transform.Position);
        //         if (distSq < minDistSq)
        //         {
        //             minDistSq = distSq;
        //             nearestEnemyPos = result.transform.position;
        //             foundTarget = true;
        //         }
        //     }
        //     physicsState.ValueRO.GetInRadius(transform.Position, projectile.Radius, physicsState.ValueRO.EnemyGhostLayer, out BodiesInRadius ghostInRadius);
        //     foreach (FindObjectsResult result in ghostInRadius.enumerator)
        //     {
        //         float distSq = math.distancesq(result.transform.position, transform.Position);
        //         if (distSq < minDistSq)
        //         {
        //             minDistSq = distSq;
        //             nearestEnemyPos = result.transform.position;
        //             foundTarget = true;
        //         }
        //     }
        //
        //     if (foundTarget)
        //     {
        //         float3 currentDir = math.normalize(transform.Forward());
        //         float3 targetDir = math.normalizesafe(MathsBurst.DimSwitcher(nearestEnemyPos - transform.Position, dim3));
        //         float speed = math.length(velocity.Linear);
        //         float3 newDir = MathsBurst.RotateVectorTowards(currentDir, targetDir, turnSpeed * dt);
        //         transform.Rotation = quaternion.LookRotationSafe(newDir, math.up());
        //         velocity.Linear = newDir * speed;
        //         transformRef.ValueRW = transform;
        //         velocityRef.ValueRW = velocity;
        //     }
        // }
    }
}