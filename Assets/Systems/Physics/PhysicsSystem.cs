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
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;
using static Unity.Mathematics.math;
using CapsuleCollider = Latios.Psyshock.CapsuleCollider;
using Collider = Latios.Psyshock.Collider;
using Physics = Latios.Psyshock.Physics;


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
        
        if (Dim == Dimension.Two)
        {
            GeometryHelper.ProjectPoint2D(
                projectDirection2D, t.Position, out projectedPosition);
            GeometryHelper.ProjectCollider2D(
                projectDirection2D, entityColliders[i], t.Rotation, out projectedCollider);
        }

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

[BurstCompile]
public struct PhysicsComponentLookups {
    public PhysicsComponentLookup<Unity.Physics.PhysicsVelocity> velocity;
    public PhysicsComponentLookup<Unity.Physics.PhysicsMass> mass;
    public PhysicsComponentLookup<LocalTransform> transform;

    public PhysicsComponentLookup<PlayerData> PlayerLookup;
    public PhysicsComponentLookup<EnemyStats> EnemyLookup;
    public PhysicsComponentLookup<PlayerProjectile> PlayerWeaponLookup;
    public PhysicsComponentLookup<DamagePlayer> EnemyWeaponLookup;
    public PhysicsComponentLookup<Obstacle> TerrainLookup;
    public PhysicsComponentLookup<Intel> IntelLookup;

    public void Update(ref SystemState state) {
        velocity.Update(ref state);
        mass.Update(ref state);
        transform.Update(ref state);
        PlayerLookup.Update(ref state);
        EnemyLookup.Update(ref state);
        PlayerWeaponLookup.Update(ref state);
        EnemyWeaponLookup.Update(ref state);
        TerrainLookup.Update(ref state);
        IntelLookup.Update(ref state);
    }
}

// Collision handling implementation.
[BurstCompile]
public struct PairsProcessor: IFindPairsProcessor {
    public PhysicsComponentLookups componentLookups;
    public EntityCommandBuffer.ParallelWriter Ecb;

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
        if (componentLookups.PlayerLookup.HasComponent(entityA) && componentLookups.EnemyWeaponLookup.HasComponent(entityB)) // Enemy Hit Player
        {
            DamagePlayer enemyProj = componentLookups.EnemyWeaponLookup.GetRW(entityB).ValueRW;
            PlayerData player = componentLookups.PlayerLookup.GetRW(entityA).ValueRW;
            player.LastDamage += enemyProj.Damage;
            componentLookups.PlayerLookup.GetRW(entityA).ValueRW = player;
            if (enemyProj.DieOnHit)
            {
                Ecb.DestroyEntity(0, entityB);
            }
        }
        else if (componentLookups.EnemyLookup.HasComponent(entityA) && componentLookups.PlayerWeaponLookup.HasComponent(entityB)) // Player Hit Enemy
        {
            PlayerProjectile playerProj = componentLookups.PlayerWeaponLookup.GetRW(entityB).ValueRW;
            EnemyStats enemy = componentLookups.EnemyLookup.GetRW(entityA).ValueRW;
            enemy.Health -= playerProj.Stats.damage;

            playerProj.Health -= (int)math.ceil(10000f / (1+playerProj.Stats.pierce));
                
            componentLookups.EnemyLookup.GetRW(entityA).ValueRW = enemy;
            componentLookups.PlayerWeaponLookup.GetRW(entityB).ValueRW = playerProj;
                
            if (playerProj.Health == 0)
            {
                Ecb.DestroyEntity(0, entityB);
            }
        }
        else if (componentLookups.EnemyWeaponLookup.HasComponent(entityA) && componentLookups.PlayerWeaponLookup.HasComponent(entityB))
        {
            DamagePlayer enemyProj = componentLookups.EnemyWeaponLookup.GetRW(entityA).ValueRW;
            PlayerProjectile playerProj = componentLookups.PlayerWeaponLookup.GetRW(entityB).ValueRW;
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
                
                componentLookups.EnemyWeaponLookup.GetRW(entityA).ValueRW = enemyProj;
                componentLookups.PlayerWeaponLookup.GetRW(entityB).ValueRW = playerProj;
                
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
        else if (componentLookups.TerrainLookup.HasComponent(entityA) && componentLookups.EnemyWeaponLookup.HasComponent(entityB))
        {
            Ecb.DestroyEntity(0, entityB);
        }
        else if (componentLookups.TerrainLookup.HasComponent(entityA) && componentLookups.PlayerWeaponLookup.HasComponent(entityB))
        {
            Ecb.DestroyEntity(0, entityB);
        }
    }
}

// Enumerator implementation for iterating over bodies within a radius
[BurstCompile]
public struct BodiesInRadius {
    public FindObjectsEnumerator enumerator;
    public (FindObjectsResult, PointDistanceResult) current;
    public float3 point;
    public float radius;

    public object Current {
        [BurstCompile]
        get {
            return current;
        } 
    }

    [BurstCompile]
    public bool MoveNext() {
        while (enumerator.MoveNext()) {
            var objectResult = enumerator.Current;
            PointDistanceResult distanceResult;
            if (Physics.DistanceBetween(point, objectResult.collider, objectResult.transform, radius, out distanceResult)) {
                current = (objectResult, distanceResult);
                return true;
            }
        }
        return false;
    }

    [BurstCompile]
    public BodiesInRadius GetEnumerator() {
        return this;
    }
}

[BurstCompile]
public struct PhysicsSystemState: IComponentData {
    // Collision layer with all physics objects
    public CollisionLayer collisionLayer;

    // Collision layers with specific types of objects
    public CollisionLayer playerLayer;
    public CollisionLayer playerWeaponLayer;
    public CollisionLayer enemyLayer;
    public CollisionLayer enemyWeaponLayer;
    public CollisionLayer terrainLayer;
    public CollisionLayer intelLayer;

    public Dimension dimension;
    public float3 projectDirection2D;

    [BurstCompile]
    public void GetInRadius(in float3 _point, float radius, out BodiesInRadius bodies) {
        float3 point = _point;
        if (dimension == Dimension.Two) {
            GeometryHelper.ProjectPoint2D(projectDirection2D, point, out point);
        }
        var radiusAabb = new Aabb(point + new float3(-radius), point + new float3(radius));
        var candidates = Physics.FindObjects(radiusAabb, collisionLayer);

        bodies = new BodiesInRadius {
            enumerator = Physics.FindObjects(radiusAabb, collisionLayer),
            point = point,
            radius = radius
        };
    }

    [BurstCompile]
    public void Dispose() {
        collisionLayer.Dispose();
        playerLayer.Dispose();
        playerWeaponLayer.Dispose();
        enemyLayer.Dispose();
        enemyWeaponLayer.Dispose();
        terrainLayer.Dispose();
        intelLayer.Dispose();
    }
}

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct PhysicsSystem: ISystem {
    LatiosWorldUnmanaged latiosWorld;

    PhysicsComponentLookups componentLookups;

    EntityQuery physicsObjectQuery;

    EntityQuery playerQuery;
    EntityQuery playerWeaponQuery;
    EntityQuery enemyQuery;
    EntityQuery enemyWeaponQuery;
    EntityQuery terrainQuery;
    EntityQuery intelQuery;

    float3 projectDirection2D;

    [BurstCompile]
    private JobHandle BuildLayer(
            in Aabb worldBounds, in Dimension dim,
            in float3 projectDirection2D, in EntityQuery query,
            out CollisionLayer collisionLayer)
    {
        var entities = query.ToEntityArray(Allocator.TempJob);
        var colliderBodies = new NativeArray<ColliderBody>(entities.Length, Allocator.TempJob);
        var entityColliders = query.ToComponentDataArray<Collider>(Allocator.TempJob);
        var entityTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

        var colliderBodiesDependency = new ColliderBodiesJob {
            projectDirection2D = projectDirection2D,
            colliderBodies = colliderBodies,
            entities = entities,
            entityColliders = entityColliders,
            entityTransforms = entityTransforms,
            Dim = dim
        }.Schedule(entities.Length, 64);
        colliderBodiesDependency = entities.Dispose(colliderBodiesDependency);
        colliderBodiesDependency = entityColliders.Dispose(colliderBodiesDependency);
        colliderBodiesDependency = entityTransforms.Dispose(colliderBodiesDependency);

        var physicsLayerDependency = Physics.BuildCollisionLayer(colliderBodies)
            .WithSubdivisions(5, 5, 5).WithWorldBounds(worldBounds)
            .ScheduleParallel(out collisionLayer, Allocator.Persistent, colliderBodiesDependency);
        physicsLayerDependency = colliderBodies.Dispose(physicsLayerDependency);

        return physicsLayerDependency;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        latiosWorld = state.GetLatiosWorldUnmanaged();

        componentLookups = new PhysicsComponentLookups {
            velocity = state.GetComponentLookup<Unity.Physics.PhysicsVelocity>(),
            mass = state.GetComponentLookup<Unity.Physics.PhysicsMass>(),
            transform = state.GetComponentLookup<LocalTransform>(),
            
            PlayerLookup = state.GetComponentLookup<PlayerData>(),
            EnemyWeaponLookup = state.GetComponentLookup<DamagePlayer>(),
            EnemyLookup = state.GetComponentLookup<EnemyStats>(),
            PlayerWeaponLookup = state.GetComponentLookup<PlayerProjectile>(),
            TerrainLookup = state.GetComponentLookup<Obstacle>(),
            IntelLookup = state.GetComponentLookup<Intel>()
        };

        // Top-down 2D projection
        projectDirection2D = new float3(0f, 1f, 0f);

        physicsObjectQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);

        playerQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<PlayerData>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);

        playerWeaponQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<PlayerProjectile>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);

        enemyQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<EnemyStats>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);

        enemyWeaponQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<DamagePlayer>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);

        terrainQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<Obstacle>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);

        intelQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<Intel>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);
        

        state.EntityManager.AddComponent<PhysicsSystemState>(state.SystemHandle);
        SystemAPI.SetComponent(state.SystemHandle, new PhysicsSystemState {
            projectDirection2D = projectDirection2D
        });
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) {
        RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
        physicsState.ValueRW.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
        physicsState.ValueRW.dimension = DimensionManager.burstDim.Data;

        // Create world bounds centered on the player
        var playerEntity = SystemAPI.GetSingletonEntity<PlayerData>();
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
        var worldBoundHalfSize = new float3(100);
        var playerAabb = new Aabb(
                playerTransform.Position - worldBoundHalfSize,
                playerTransform.Position + worldBoundHalfSize);

        physicsState.ValueRW.Dispose();

        var buildMainLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                physicsObjectQuery, out physicsState.ValueRW.collisionLayer);

        var buildPlayerLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                playerQuery, out physicsState.ValueRW.playerLayer);

        var buildPlayerWeaponLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                playerWeaponQuery, out physicsState.ValueRW.playerWeaponLayer);

        var buildEnemyLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                enemyQuery, out physicsState.ValueRW.enemyLayer);

        var buildEnemyWeaponLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                enemyWeaponQuery, out physicsState.ValueRW.enemyWeaponLayer);

        var buildTerrainLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                terrainQuery, out physicsState.ValueRW.terrainLayer);

        var buildIntelLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                intelQuery, out physicsState.ValueRW.intelLayer);

        var buildLayers = JobHandle.CombineDependencies(
                buildMainLayer, buildPlayerLayer, buildPlayerWeaponLayer);
        buildLayers = JobHandle.CombineDependencies(
                buildLayers, buildEnemyLayer, buildEnemyWeaponLayer);
        buildLayers = JobHandle.CombineDependencies(
                buildLayers, buildTerrainLayer, buildIntelLayer);

        componentLookups.Update(ref state);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var ecbWriter = ecb.AsParallelWriter();


        var pairsProcessor = new PairsProcessor {
            componentLookups = componentLookups,
            Ecb = ecbWriter
        };

        // Collide enemy weapons with player
        var Dependency = Physics.FindPairs(
                physicsState.ValueRO.enemyWeaponLayer,
                physicsState.ValueRO.playerLayer, pairsProcessor)
            .ScheduleParallel(buildLayers);
        // Collide enemies with enemies
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.enemyLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide player weapons with enemies
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerWeaponLayer,
                physicsState.ValueRO.enemyLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide player weapons with enemy weapons
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerWeaponLayer,
                physicsState.ValueRO.enemyLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collider player with intel
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerLayer,
                physicsState.ValueRO.intelLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide player with terrain
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerLayer,
                physicsState.ValueRO.terrainLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide enemies with terrain
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.enemyLayer,
                physicsState.ValueRO.terrainLayer, pairsProcessor)
            .ScheduleParallel(Dependency);

        JobHandle.ScheduleBatchedJobs();
        Dependency.Complete();
       
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        /*
        // Example of using GetColliderBodiesInRange to draw the colliders on
        // all the objects within 100 units of the origin
        BodiesInRadius inRadius;
        physicsState.ValueRO.GetInRadius(new float3(0f), 100f, out inRadius);
        foreach ((FindObjectsResult, PointDistanceResult) result in inRadius) {
            PhysicsDebug.DrawCollider(result.Item1.collider, result.Item1.transform, UnityEngine.Color.red);
        }
        */

        // Draw bounding box gizmos
        // PhysicsDebug.DrawLayer(physicsState.ValueRO.collisionLayer).Run();
    }
    
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}
