/*
 * 2D collision detection system using Psyshock physics from Latios framework.
 *
 * TODO:
 * - Projections for all the primitive colliders
 * - 2D collision handling should mimic the behaviour of 3D collisions
 */

using Enemies.AI;
using Latios;
using Latios.Psyshock;
using Latios.Transforms;

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using static Unity.Mathematics.math;


// Static helper methods
[BurstCompile]
public struct GeometryHelper {
    // Project `point` onto the plane with `projectDirection2D` as its normal
    [BurstCompile]
    public static void ProjectPoint2D(
            in float3 projectDirection2D, in float3 point, out float3 projected)
    {
        projected = point - project(point, projectDirection2D);
    }

    // Project `c` onto the plane with `projectDirection2D` as its normal.
    //
    // The projection needs to take the collider's rotation into account so that
    // the projected collider works as intended after its transformation is applied.
    [BurstCompile]
    public static void ProjectCollider2D(
            in float3 projectDirection2D, in Collider c,
            in quaternion rotation, out Collider projected)
    {
        projected = c;

        switch (c.type) {
            case ColliderType.Sphere:
                // Spheres stay the same
                break;
            case ColliderType.Capsule:
                // Capsules have both points transformed by the projection
                CapsuleCollider capsule = (CapsuleCollider) c;
                float3 rotatedDirection = mul(inverse(rotation), projectDirection2D);
                float3 pointA;
                ProjectPoint2D(rotatedDirection, capsule.pointA, out pointA);
                float3 pointB;
                ProjectPoint2D(rotatedDirection, capsule.pointB, out pointB);
                projected = new CapsuleCollider(
                        pointA, pointB,
                        capsule.radius, capsule.stretchMode);
                break;
            // TODO: support for box colliders
        }
    }

    // Project the given collider and position into 2D, if the given `Dimension`
    // is 2D. Otherwise, the collider and position are unchanged.
    [BurstCompile]
    public static void Project2D(
            in Dimension dim, in float3 projectDirection2D, in Collider c,
            in float3 position, in quaternion rotation,
            out Collider projectedCollider, out float3 projectedPosition)
    {
        if (dim == Dimension.Two) {
            GeometryHelper.ProjectPoint2D(
                projectDirection2D, position, out projectedPosition);
            GeometryHelper.ProjectCollider2D(
                projectDirection2D, c, rotation, out projectedCollider);
        } else {
            projectedCollider = c;
            projectedPosition = position;
        }
    }
}

// Initializes the array of `ColliderBody` that gets used to build the Latios
// `CollisionLayer`. The input colliders are projected into 2D so that the
// `CollisionLayer` can be used to query 2D collisions.
[BurstCompile]
partial struct ColliderBodiesJob: IJobParallelFor {
    public float3 projectDirection2D;
    public NativeArray<ColliderBody> colliderBodies;
    public NativeArray<Entity> entities;
    public NativeArray<LocalTransform> entityTransforms;
    public NativeArray<Collider> entityColliders;
    public Dimension Dim;

    [BurstCompile]
    public void Execute(int i) {
        var t = entityTransforms[i];
        float3 projectedPosition = t.Position;
        Collider projectedCollider = entityColliders[i];

        GeometryHelper.Project2D(
                Dim, projectDirection2D, entityColliders[i],
                t.Position, t.Rotation,
                out projectedCollider, out projectedPosition);

        colliderBodies[i] = new ColliderBody {
            collider = projectedCollider,
            transform = new TransformQvvs(projectedPosition, t.Rotation, 1, t.Scale),
            entity = entities[i]
        };

        //PhysicsDebug.DrawCollider(colliderBodies[i].collider, colliderBodies[i].transform, UnityEngine.Color.red);
    }
}

// Build a mapping from each `Entity` to the index of its `ColliderBody` in the
// current collision layer
[BurstCompile]
partial struct BodyIndicesJob: IJobParallelFor {
    public NativeArray<ColliderBody>.ReadOnly colliderBodies;
    public NativeParallelHashMap<Entity, int>.ParallelWriter writer;

    [BurstCompile]
    public void Execute(int i) {
        writer.TryAdd(colliderBodies[i].entity, i);
    }
}

// Helpers to find objects within a radius
public interface IRadiusProcessor {
    // objectResult stores the object that was within the radius
    // distanceResult stores information about how close the object was, etc.
    void Execute(in FindObjectsResult objectResult, in PointDistanceResult distanceResult);
}

// An implementation of IRadiusProcessor that draws the collider (for testing)
[BurstCompile]
public struct DebugDrawRadiusProcessor: IRadiusProcessor {
    [BurstCompile]
    public void Execute(in FindObjectsResult objectResult, in PointDistanceResult _) {
        PhysicsDebug.DrawCollider(objectResult.collider, objectResult.transform, UnityEngine.Color.red);
    }
}

// Collision handling implementation.
[BurstCompile]
public struct PairsProcessor: IFindPairsProcessor {
    public PhysicsComponentLookup<Unity.Physics.PhysicsVelocity> velocity;
    public PhysicsComponentLookup<Unity.Physics.PhysicsMass> mass;
    public PhysicsComponentLookup<LocalTransform> transform;
    
    public PhysicsComponentLookup<PlayerData> PlayerLookup;
    public PhysicsComponentLookup<EnemyStats> EnemyLookup;
    public PhysicsComponentLookup<PlayerProjectile> PlayerWeaponLookup;
    public PhysicsComponentLookup<DamagePlayer> EnemyWeaponLookup;
    public PhysicsComponentLookup<Obstacle> TerrainLookup;
        
    public EntityCommandBuffer.ParallelWriter Ecb;

    public void Update(ref SystemState state)
    {
        velocity.Update(ref state);
        mass.Update(ref state);
        transform.Update(ref state);
        PlayerLookup.Update(ref state);
        EnemyLookup.Update(ref state);
        PlayerWeaponLookup.Update(ref state);
        EnemyWeaponLookup.Update(ref state);
        TerrainLookup.Update(ref state);
    }
    
    public void Execute(in FindPairsResult result) {
        ColliderDistanceResult r;
        if (Physics.DistanceBetween(
                    result.bodyA.collider, result.bodyA.transform,
                    result.bodyB.collider, result.bodyB.transform,
                    0, out r))
        {
            Calculate(result.entityA, result.entityB);
            Calculate(result.entityB, result.entityA);
        }
    }
    
    [BurstCompile]
    private void Calculate(SafeEntity entityA, SafeEntity entityB)
    {
        //Debug.Log($"OK! {EnemyLookup.HasComponent(entityA)} {PlayerWeaponLookup.HasComponent(entityB)}");
        if (PlayerLookup.HasComponent(entityA) && EnemyWeaponLookup.HasComponent(entityB)) // Enemy Hit Player
        {
            DamagePlayer enemyProj = EnemyWeaponLookup.GetRW(entityB).ValueRW;
            PlayerData player = PlayerLookup.GetRW(entityA).ValueRW;
            player.LastDamage += enemyProj.Damage;
            PlayerLookup.GetRW(entityA).ValueRW = player;
            if (enemyProj.DieOnHit)
            {
                Ecb.DestroyEntity(0, entityB);
            }
        }
        else if (EnemyLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB)) // Player Hit Enemy
        {
            PlayerProjectile playerProj = PlayerWeaponLookup.GetRW(entityB).ValueRW;
            EnemyStats enemy = EnemyLookup.GetRW(entityA).ValueRW;
            enemy.Health -= playerProj.Stats.damage;

            playerProj.Health -= (int)math.ceil(10000f / (1+playerProj.Stats.pierce));
                
            EnemyLookup.GetRW(entityA).ValueRW = enemy;
            PlayerWeaponLookup.GetRW(entityB).ValueRW = playerProj;
                
            if (playerProj.Health == 0)
            {
                Ecb.DestroyEntity(0, entityB);
            }
        }
        else if (EnemyWeaponLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB))
        {
            DamagePlayer enemyProj = EnemyWeaponLookup.GetRW(entityA).ValueRW;
            PlayerProjectile playerProj = PlayerWeaponLookup.GetRW(entityB).ValueRW;
            if (enemyProj.Mass == -1)
            {
                Ecb.DestroyEntity(0, entityB);
            }
            else
            {
                while (enemyProj.Mass > 0 && playerProj.Health > 0)
                {
                    enemyProj.Mass--;
                    playerProj.Health -= (int)math.ceil(10000f / ((1+playerProj.Stats.pierce)*(1+playerProj.Stats.power)));
                }
                
                EnemyWeaponLookup.GetRW(entityA).ValueRW = enemyProj;
                PlayerWeaponLookup.GetRW(entityB).ValueRW = playerProj;
                
                if (enemyProj.Mass == 0)
                {
                    Ecb.DestroyEntity(0, entityA);
                }
                if (playerProj.Health == 0)
                {
                    Ecb.DestroyEntity(0, entityB);
                }
            }
        }
        else if (TerrainLookup.HasComponent(entityA) && EnemyWeaponLookup.HasComponent(entityB))
        {
            Ecb.DestroyEntity(0, entityB);
        }
        else if (TerrainLookup.HasComponent(entityA) && PlayerWeaponLookup.HasComponent(entityB))
        {
            Ecb.DestroyEntity(0, entityB);
        }
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct PhysicsSystem: ISystem {
    LatiosWorldUnmanaged latiosWorld;

    NativeParallelHashMap<Entity, int> entitiesToBodyIndices;
    CollisionLayer collisionLayer;
    PairsProcessor pairsProcessor;

    EntityQuery physicsObjectQuery;

    float3 projectDirection2D;

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        latiosWorld = state.GetLatiosWorldUnmanaged();
        
        pairsProcessor = new PairsProcessor {
            velocity = state.GetComponentLookup<Unity.Physics.PhysicsVelocity>(),
            mass = state.GetComponentLookup<Unity.Physics.PhysicsMass>(),
            transform = state.GetComponentLookup<LocalTransform>(),
            
            PlayerLookup = state.GetComponentLookup<PlayerData>(),
            EnemyWeaponLookup = state.GetComponentLookup<DamagePlayer>(),
            EnemyLookup = state.GetComponentLookup<EnemyStats>(),
            PlayerWeaponLookup = state.GetComponentLookup<PlayerProjectile>(),
            TerrainLookup = state.GetComponentLookup<Obstacle>(),
        };

        // Top-down 2D projection
        projectDirection2D = new float3(0f, 1f, 0f);

        var i = 0;
        var componentTypes = new NativeArray<ComponentType>(4, Allocator.Temp);
        componentTypes[i++] = ComponentType.ReadOnly<Collider>();
        componentTypes[i++] = ComponentType.ReadWrite<Unity.Physics.PhysicsVelocity>();
        componentTypes[i++] = ComponentType.ReadOnly<Unity.Physics.PhysicsMass>();
        componentTypes[i++] = ComponentType.ReadWrite<LocalTransform>();
        physicsObjectQuery = state.GetEntityQuery(componentTypes);

        entitiesToBodyIndices = new NativeParallelHashMap<Entity, int>(0, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) {
        collisionLayer.Dispose();
        entitiesToBodyIndices.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        // Temporary allocations needed to build the collision layer
        var entities = physicsObjectQuery.ToEntityArray(Allocator.TempJob);
        var colliderBodies = new NativeArray<ColliderBody>(entities.Length, Allocator.TempJob);
        var entityColliders = physicsObjectQuery.ToComponentDataArray<Collider>(Allocator.TempJob);
        var entityTransforms = physicsObjectQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        // Initialize the array of `ColliderBody`
        new ColliderBodiesJob {
            projectDirection2D = projectDirection2D,
            colliderBodies = colliderBodies,
            entities = entities,
            entityColliders = entityColliders,
            entityTransforms = entityTransforms,
            Dim = DimensionManager.burstDim.Data
        }.Schedule(entities.Length, 64).Complete();

        // Delete the previous collision layer
        collisionLayer.Dispose();
        // Build the current collision layer
        Physics.BuildCollisionLayer(colliderBodies).WithSubdivisions(32, 5, 32).WithWorldBounds(-1000, 1000)
            .ScheduleParallel(out collisionLayer, Allocator.Persistent).Complete();

        // update the mapping from entities to their body indices
        entitiesToBodyIndices.Clear();
        if (entities.Length > entitiesToBodyIndices.Capacity) {
            entitiesToBodyIndices.Capacity = entities.Length * 2;
        }
        new BodyIndicesJob {
            colliderBodies = collisionLayer.colliderBodies,
            writer = entitiesToBodyIndices.AsParallelWriter()
        }.Schedule(entities.Length, 64).Complete();

        // Run the pairs processor to handle collisions
        pairsProcessor.Update(ref state);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        pairsProcessor.Ecb = ecb.AsParallelWriter();
        
        Physics.FindPairs(collisionLayer, pairsProcessor)
            .ScheduleParallel().Complete();
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        /*
        // Example of using GetColliderBodiesInRange to draw the colliders on
        // all the objects within 100 units of the origin
        var ddrp = new DebugDrawRadiusProcessor {};
        GetColliderBodiesInRange(new float3(0f), 100f, ddrp);
        */

        // Dispose of temporary allocations
        entities.Dispose();
        colliderBodies.Dispose();
        entityColliders.Dispose();
        entityTransforms.Dispose();
    }
    
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }

    // Get the collider body associated with the given entity in the current
    // collisionlayer
    public bool GetColliderBody(in Entity entity, out ColliderBody body) {
        if (entitiesToBodyIndices.ContainsKey(entity)) {
            body = collisionLayer.colliderBodies[entitiesToBodyIndices[entity]];
            return true;
        } else {
            body = new ColliderBody {};
            return false;
        }
    }

    // Get the collider bodies within the range
    [BurstCompile]
    public void GetColliderBodiesInRange<T>(in float3 _point, float radius, in T processor) where T: struct, IRadiusProcessor {
        float3 point = _point;
        if (DimensionManager.burstDim.Data == Dimension.Two) {
            GeometryHelper.ProjectPoint2D(projectDirection2D, point, out point);
        }
        var radiusAabb = new Aabb(point + new float3(-radius), point + new float3(radius));
        var candidates = Physics.FindObjects(radiusAabb, collisionLayer);
        foreach (ref readonly FindObjectsResult objectResult in candidates) {
            PointDistanceResult distanceResult;
            if (Physics.DistanceBetween(point, objectResult.collider, objectResult.transform, radius, out distanceResult)) {
                processor.Execute(objectResult, distanceResult);
            }
        }
    }
}
