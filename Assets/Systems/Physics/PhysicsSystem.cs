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
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Mathematics.math;
using CapsuleCollider = Latios.Psyshock.CapsuleCollider;
using Collider = Latios.Psyshock.Collider;
using Physics = Latios.Psyshock.Physics;
using SphereCollider = Latios.Psyshock.SphereCollider;


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
            transform = new TransformQvvs(projectedPosition, t.Rotation, t.Scale, 1),
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
    public PhysicsComponentLookup<PlayerProjectileDeath> PlayerWeaponStatsLookup;
    public PhysicsComponentLookup<EnemyCollisionReceiver> EnemyLookup;
    public PhysicsComponentLookup<PlayerProjectile> PlayerWeaponLookup;
    public PhysicsComponentLookup<DamagePlayer> EnemyWeaponLookup;
    public PhysicsComponentLookup<Obstacle> TerrainLookup;
    public PhysicsComponentLookup<Intel> IntelLookup;
    public PlayerProjectileEffectLookups PlayerProjectileEffects;

    public void Update(ref SystemState state) {
        velocity.Update(ref state);
        mass.Update(ref state);
        transform.Update(ref state);
        PlayerLookup.Update(ref state);
        EnemyLookup.Update(ref state);
        PlayerWeaponLookup.Update(ref state);
        PlayerWeaponStatsLookup.Update(ref state);
        EnemyWeaponLookup.Update(ref state);
        TerrainLookup.Update(ref state);
        IntelLookup.Update(ref state);
        PlayerProjectileEffects.Update(ref state);
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
        get => current;
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
    public CollisionLayer PlayerLayer;
    public CollisionLayer PlayerInteractLayer;
    public CollisionLayer PlayerWeaponLayer;
    public CollisionLayer EnemyLayer;
    public CollisionLayer EnemyGhostLayer;
    public CollisionLayer EnemyWeaponLayer;
    public CollisionLayer TerrainLayer;
    public CollisionLayer IntelLayer;

    public Dimension dimension;
    public float3 projectDirection2D;

    [BurstCompile]
    public void GetInRadius(in float3 _point, float radius, CollisionLayer layer, out BodiesInRadius bodies) {
        float3 point = _point;
        if (dimension == Dimension.Two) {
            GeometryHelper.ProjectPoint2D(projectDirection2D, point, out point);
        }
        var radiusAabb = new Aabb(point + new float3(-radius), point + new float3(radius));
        var candidates = Physics.FindObjects(radiusAabb, layer);

        bodies = new BodiesInRadius {
            enumerator = candidates,
            point = point,
            radius = radius
        };
    }

    [BurstCompile]
    public void Dispose() {
        collisionLayer.Dispose();
        PlayerLayer.Dispose();
        PlayerInteractLayer.Dispose();
        PlayerWeaponLayer.Dispose();
        EnemyLayer.Dispose();
        EnemyGhostLayer.Dispose();
        EnemyWeaponLayer.Dispose();
        TerrainLayer.Dispose();
        IntelLayer.Dispose();
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
    EntityQuery enemyGhostedQuery;
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
        return BuildLayer(worldBounds, dim, projectDirection2D, query, out collisionLayer, new NativeArray<ColliderBody>());
    }

    [BurstCompile]
    private JobHandle BuildLayer(
            in Aabb worldBounds, in Dimension dim,
            in float3 projectDirection2D, in EntityQuery? query,
            out CollisionLayer collisionLayer, NativeArray<ColliderBody>? bodies)
    {
        var size = 0;
        if (bodies.HasValue) size += bodies.Value.Length;
        NativeArray<Entity> entities;
        NativeArray<ColliderBody> colliderBodies;
        JobHandle dependency = new JobHandle();
        if (query.HasValue)
        {
            entities = query.Value.ToEntityArray(Allocator.TempJob);
            size += entities.Length;
            colliderBodies = new NativeArray<ColliderBody>(size, Allocator.TempJob);
            
            var entityColliders = query.Value.ToComponentDataArray<Collider>(Allocator.TempJob);
            var entityTransforms = query.Value.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
            
            dependency = new ColliderBodiesJob {
                projectDirection2D = projectDirection2D,
                colliderBodies = colliderBodies,
                entities = entities,
                entityColliders = entityColliders,
                entityTransforms = entityTransforms,
                Dim = dim
            }.Schedule(entities.Length, 64);
            
            dependency = entities.Dispose(dependency);
            dependency = entityColliders.Dispose(dependency);
            dependency = entityTransforms.Dispose(dependency);

            if (bodies.HasValue)
            {
                for (var i = 0; i < bodies.Value.Length; i++)
                {
                    colliderBodies[entities.Length + i] = bodies.Value[i];
                }
            }
        }
        else
        {
            colliderBodies = bodies.Value;
        }
        
        var physicsLayerDependency = Physics.BuildCollisionLayer(colliderBodies)
            .WithSubdivisions(5, 5, 5).WithWorldBounds(worldBounds)
            .ScheduleParallel(out collisionLayer, Allocator.Persistent, dependency);
        physicsLayerDependency = colliderBodies.Dispose(physicsLayerDependency);

        return physicsLayerDependency;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state) {
        state.RequireForUpdate<PlayerData>();
        state.RequireForUpdate<VfxReceiver>();

        latiosWorld = state.GetLatiosWorldUnmanaged();
        componentLookups = new PhysicsComponentLookups {
            velocity = state.GetComponentLookup<Unity.Physics.PhysicsVelocity>(),
            mass = state.GetComponentLookup<Unity.Physics.PhysicsMass>(),
            transform = state.GetComponentLookup<LocalTransform>(),
            PlayerWeaponStatsLookup = state.GetComponentLookup<PlayerProjectileDeath>(),
            PlayerLookup = state.GetComponentLookup<PlayerData>(),
            EnemyWeaponLookup = state.GetComponentLookup<DamagePlayer>(),
            EnemyLookup = state.GetComponentLookup<EnemyCollisionReceiver>(),
            PlayerWeaponLookup = state.GetComponentLookup<PlayerProjectile>(),
            TerrainLookup = state.GetComponentLookup<Obstacle>(),
            IntelLookup = state.GetComponentLookup<Intel>()
        };
        componentLookups.PlayerProjectileEffects.Init(ref state);

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
            .WithAllRW<EnemyCollisionReceiver>()
            .WithNone<EnemyGhostedTag>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass>()
            .Build(ref state);
        
        enemyGhostedQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<EnemyCollisionReceiver>()
            .WithAll<EnemyGhostedTag>()
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
        
        if (!SystemAPI.TryGetSingleton(out VfxReceiver soundReceiver)) return;
        var soundWriter = soundReceiver.VfxCommands.AsParallelWriter();
        GeometryHelper.ProjectPoint2D(projectDirection2D, playerTransform.Position, out float3 playerCenter);
        var worldBoundHalfSize = new float3(100);
        var playerAabb = new Aabb(
            playerCenter - worldBoundHalfSize,
            playerCenter + worldBoundHalfSize);

        physicsState.ValueRW.Dispose();

        var buildMainLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                physicsObjectQuery, out physicsState.ValueRW.collisionLayer);

        var buildPlayerLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                playerQuery, out physicsState.ValueRW.PlayerLayer);

        var playerHitbox = new NativeArray<ColliderBody>(1, Allocator.TempJob);
        
        playerHitbox[0] = new()
        {
            collider = new SphereCollider(playerCenter, 30), transform = TransformQvvs.identity,
            entity = playerEntity
        };
        
        var buildPlayerInteractLayer = BuildLayer(
            playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
            null, out physicsState.ValueRW.PlayerInteractLayer, new NativeArray<ColliderBody>(playerHitbox, Allocator.TempJob));

        var buildPlayerWeaponLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                playerWeaponQuery, out physicsState.ValueRW.PlayerWeaponLayer);

        var buildEnemyLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                enemyQuery, out physicsState.ValueRW.EnemyLayer);
        
        var buildEnemyGhostLayer = BuildLayer(
            playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
            enemyGhostedQuery, out physicsState.ValueRW.EnemyGhostLayer);

        var buildEnemyWeaponLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                enemyWeaponQuery, out physicsState.ValueRW.EnemyWeaponLayer);

        var buildTerrainLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                terrainQuery, out physicsState.ValueRW.TerrainLayer);

        var buildIntelLayer = BuildLayer(
                playerAabb, DimensionManager.burstDim.Data, projectDirection2D,
                intelQuery, out physicsState.ValueRW.IntelLayer);

        var buildLayers = JobHandle.CombineDependencies(
                buildMainLayer, buildPlayerLayer, buildPlayerWeaponLayer);
        buildLayers = JobHandle.CombineDependencies(
                buildLayers, buildEnemyLayer, buildEnemyWeaponLayer);
        buildLayers = JobHandle.CombineDependencies(
                buildLayers, buildTerrainLayer, buildIntelLayer);
        buildLayers = JobHandle.CombineDependencies(
            buildLayers, buildPlayerInteractLayer, buildEnemyGhostLayer);

        componentLookups.Update(ref state);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var ecbWriter = ecb.AsParallelWriter();

        var destroyedSet = new NativeParallelHashSet<Entity>(physicsState.ValueRO.collisionLayer.colliderBodies.Length, Allocator.TempJob);
        var destroyedSetWriter = destroyedSet.AsParallelWriter();
        
        var pairsProcessor = new PairsProcessor {
            ComponentLookups = componentLookups,
            Ecb = ecbWriter,
            DeltaTime = SystemAPI.Time.fixedDeltaTime,
            Dim = physicsState.ValueRW.dimension,
            DestroyedSetWriter = destroyedSetWriter
        };
        var terrainProcessor = new TerrainPairs {
            ComponentLookups = componentLookups,
            Ecb = ecbWriter,
            DestroyedSetWriter = destroyedSetWriter
        };
        var playerWeaponPairsProcessor = new PlayerWeaponPairs {
            ComponentLookups = componentLookups,
            Ecb = ecbWriter,
            DestroyedSetWriter = destroyedSetWriter,
            AudioWriter = soundWriter,
        };
        var playerPairsProcessor = new PlayerPairs {
            ComponentLookups = componentLookups,
            Ecb = ecbWriter,
            DestroyedSetWriter = destroyedSetWriter
        };
        var playerInteractionsPairsProcessor = new PlayerInteractions {
            ComponentLookups = componentLookups,
            Ecb = ecbWriter,
            DestroyedSetWriter = destroyedSetWriter
        };
        
        // Collider player with intel
        var dependency = Physics.FindPairs(
                physicsState.ValueRO.PlayerLayer,
                physicsState.ValueRO.IntelLayer, playerPairsProcessor)
            .ScheduleParallel(buildLayers);
        
        // Temp intel vacuum
        
        
        // dependency = Physics.FindPairs(
        //         physicsState.ValueRO.PlayerInteractLayer,
        //         physicsState.ValueRO.IntelLayer, playerInteractionsPairsProcessor)
        //     .ScheduleParallel(dependency);
        
        // Collide player enemy weapons
        dependency = Physics.FindPairs(
            physicsState.ValueRO.PlayerLayer,
                physicsState.ValueRO.EnemyWeaponLayer, playerPairsProcessor)
            .ScheduleParallel(dependency);
        
        // Collide enemies with enemies
        dependency = Physics.FindPairs(
                physicsState.ValueRO.EnemyLayer, pairsProcessor)
            .ScheduleParallel(dependency);
        
        // Collide player weapons with enemies
        dependency = Physics.FindPairs(
                physicsState.ValueRO.PlayerWeaponLayer, physicsState.ValueRO.EnemyLayer, playerWeaponPairsProcessor)
            .ScheduleParallel(dependency);
        dependency = Physics.FindPairs(
                physicsState.ValueRO.PlayerWeaponLayer, physicsState.ValueRO.EnemyGhostLayer, playerWeaponPairsProcessor)
            .ScheduleParallel(dependency);
        // Collide player weapons with enemy weapons
        dependency = Physics.FindPairs(
                physicsState.ValueRO.PlayerWeaponLayer,
                physicsState.ValueRO.EnemyWeaponLayer, playerWeaponPairsProcessor)
            .ScheduleParallel(dependency);
        
        // Collide player with terrain
        dependency = Physics.FindPairs(
                physicsState.ValueRO.TerrainLayer,
                physicsState.ValueRO.PlayerLayer, terrainProcessor)
            .ScheduleParallel(dependency);
        // Collide player weapons with terrain
        dependency = Physics.FindPairs(
                physicsState.ValueRO.TerrainLayer,
                physicsState.ValueRO.PlayerWeaponLayer, terrainProcessor)
            .ScheduleParallel(dependency);
        // Collide enemies with terrain
        dependency = Physics.FindPairs(
                physicsState.ValueRO.TerrainLayer,
                physicsState.ValueRO.EnemyLayer, terrainProcessor)
            .ScheduleParallel(dependency);
        // Collide enemy weapons with terrain
        dependency = Physics.FindPairs(
                physicsState.ValueRO.TerrainLayer,
                physicsState.ValueRO.EnemyWeaponLayer, terrainProcessor)
            .ScheduleParallel(dependency);

        JobHandle.ScheduleBatchedJobs();
        dependency.Complete();
        
        //PhysicsDebug.DrawLayer(physicsState.ValueRO.PlayerLayer).Run();
        PhysicsDebug.DrawLayer(physicsState.ValueRO.EnemyLayer).Run();
        //PhysicsDebug.DrawLayer(physicsState.ValueRO.IntelLayer).Run();
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        
        foreach (Entity entity in destroyedSet) {
            state.EntityManager.DestroyEntity(entity);
        }
        destroyedSet.Dispose();
        

        // Draw bounding box gizmos
        
    }
    
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}
