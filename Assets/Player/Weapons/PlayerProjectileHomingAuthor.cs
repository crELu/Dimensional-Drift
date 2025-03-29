
using Latios.Psyshock;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

public class PlayerProjectileHomingAuthor : BaseAuthor
{
    public float radius;
    public float turnSpeed;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerProjectileHoming {Radius = radius, TurnSpeed = turnSpeed});
    }
}

public struct PlayerProjectileHoming : IComponentData
{
    public float Radius;
    public float TurnSpeed;
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
        public EntityCommandBuffer.ParallelWriter ECB;
    
        public void Execute(Entity entity, [EntityIndexInQuery] int index, in LaserTag laser)
        {
            ECB.DestroyEntity(index, entity);
        }
    }
    
    //[BurstCompile] 
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (projectile, transformRef, velocityRef) in SystemAPI.Query<PlayerProjectileHoming, RefRW<LocalTransform>, RefRW<PhysicsVelocity>>())
        {
            var dt = SystemAPI.Time.fixedDeltaTime;
            var transform = transformRef.ValueRW;
            var velocity = velocityRef.ValueRW;
            RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
            var turnSpeed = projectile.TurnSpeed;
            var nearestEnemyPos = float3.zero;
            var minDistSq = float.MaxValue;
            var foundTarget = false;
            
            physicsState.ValueRO.GetInRadius(transform.Position, projectile.Radius, physicsState.ValueRO.EnemyLayer, out BodiesInRadius enemyInRadius);
            foreach ((FindObjectsResult re, PointDistanceResult point) result in enemyInRadius)
            {
                float distSq = result.point.distance;
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    nearestEnemyPos = result.point.hitpoint;
                    foundTarget = true;
                }
            }
            physicsState.ValueRO.GetInRadius(transform.Position, projectile.Radius, physicsState.ValueRO.EnemyGhostLayer, out BodiesInRadius ghostInRadius);
            foreach ((FindObjectsResult re, PointDistanceResult point) result in ghostInRadius)
            {
                float distSq = result.point.distance;
                if (distSq < minDistSq)
                {
                    minDistSq = distSq;
                    nearestEnemyPos = result.point.hitpoint;
                    foundTarget = true;
                }
            }

            if (foundTarget)
            {
                float3 currentDir = math.normalize(transform.Forward());
                float3 targetDir = math.normalizesafe(MathsBurst.DimSwitcher(nearestEnemyPos - transform.Position,
                    DimensionManager.burstDim.Data == Dimension.Three));
                float speed = math.length(velocity.Linear);
                float3 newDir = MathsBurst.RotateVectorTowards(currentDir, targetDir, turnSpeed * dt);
                transform.Rotation = quaternion.LookRotationSafe(newDir, math.up());
                velocity.Linear = newDir * speed;
                transformRef.ValueRW = transform;
                velocityRef.ValueRW = velocity;
            }
        }
    }
}