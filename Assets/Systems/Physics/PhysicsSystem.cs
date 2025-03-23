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
    [ReadOnly]
    public ComponentLookup<PostTransformMatrix> postTransformLookup;
    public NativeArray<Collider> entityColliders;
    public Dimension Dim;

    [BurstCompile]
    public void Execute(int i) {
        var t = entityTransforms[i];
        float3 scale = new float3(t.Scale);
        if (postTransformLookup.HasComponent(entities[i])) {
            float4x4 postTransform = postTransformLookup.GetRefRO(entities[i]).ValueRO.Value;
            scale = new float3(
                length(new float3(postTransform.c0.x, postTransform.c1.x, postTransform.c2.x)),
                length(new float3(postTransform.c0.y, postTransform.c1.y, postTransform.c2.y)),
                length(new float3(postTransform.c0.z, postTransform.c1.z, postTransform.c2.z)));
        }
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
            // XXX: this only works for uniform scale for now...
            transform = new TransformQvvs(projectedPosition, t.Rotation, scale.x, 1),
            entity = entities[i]
        };

        // XXX: debug draw colliders to check if things aren't broken. This adds
        // lag so it's commented out most of the time.
        // PhysicsDebug.DrawCollider(colliderBodies[i].collider, colliderBodies[i].transform, UnityEngine.Color.red);
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
    public PhysicsComponentLookup<Turnips.TurnipTag> TerrainLookup;
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
    public NativeParallelHashSet<Entity>.ParallelWriter destroyedSetWriter;

    public void Execute(in FindPairsResult result) {
        ColliderDistanceResult r;
        if (Physics.DistanceBetween(
                    result.bodyA.collider, result.bodyA.transform,
                    result.bodyB.collider, result.bodyB.transform,
                    0, out r))
        {
            Calculate(result);
        }
    }

    /*
     * Do a collision check for a turnip. Turnip colliders are assumed to be a
     * bounding sphere on the whole turnip. The turnip is taller than it is wide,
     * so the diameter of the turnip collider is assumed to be the height of the
     * turnip.
     *
     * Return a vector representing how body has penetrated turnipBody, or the
     * zero vector if there is no intersection.
     */
    [BurstCompile]
    private void TurnipCollisionCheck(in ColliderBody turnipBody, in ColliderBody body, out float3 penetration) {
        penetration = new float3(0);

        // Ratio between width of turnip and height of turnip
        float widthRatio = 0.446428571f;
        // Ratio between height of "body" of turnip and total height of turnip
        float heightRatio = 0.241071429f;
        // Ratio between width of "stalk" of turnip in the middle of the body
        float stalkRatio = 0.087857143f;

        Aabb bodyAabb = Physics.AabbFrom(body.collider, body.transform);
        float bodyRadius = max(
                max(bodyAabb.max.x - bodyAabb.min.x,
                bodyAabb.max.y - bodyAabb.min.y),
                bodyAabb.max.z - bodyAabb.min.z) / 2;

        Aabb turnipAabb = Physics.AabbFrom(turnipBody.collider, turnipBody.transform);
        float turnipRadius = (turnipAabb.max.x - turnipAabb.min.x) / 2;

        float turnipBodyWidth = turnipRadius * widthRatio;
        float turnipBodyHeight = turnipRadius * heightRatio;

        float3 position = body.transform.position;
        float3 turnipPosition = turnipBody.transform.position;

        // body position relative to turnip
        float3 turnipRelativePosition = position - turnipPosition;

        // Check for intersection with turnip "body"
        // squared height of turnip at player posiiton
        float turnipHeightSquared =
            (1
            - square(turnipRelativePosition.x / turnipBodyWidth)
            - square(turnipRelativePosition.z / turnipBodyWidth))
            * square(turnipBodyHeight);

        if (turnipHeightSquared > 0) {
            float turnipHeight = sqrt(turnipHeightSquared);
            // Minimum distance the body can be from the turnip
            // without intersecting
            float minBodyDistance = bodyRadius + turnipHeight;
            if (abs(turnipRelativePosition.y) < minBodyDistance) {
                penetration.y +=
                    normalize(turnipRelativePosition).y
                    * (minBodyDistance - abs(turnipRelativePosition.y));
            }
        }

        // check for intersection with turnip "stalk"
        float turnipStalkRadius = lerp(turnipRadius * stalkRatio, 0, abs(turnipRelativePosition.y) / turnipRadius);
        // Minimum distance the body can be from the turnip
        // without intersecting
        float minStalkDistance = bodyRadius + turnipStalkRadius;
        float3 diff = turnipRelativePosition;
        diff.y = 0;
        if (length(diff) < minStalkDistance) {
            penetration += normalize(diff) * (minStalkDistance - length(diff));
        }
    }
    
    [BurstCompile]
    private void Calculate(FindPairsResult result)
    {
        SafeEntity entityA = result.entityA;
        SafeEntity entityB = result.entityB;

        if (
                componentLookups.PlayerLookup.HasComponent(entityB)
                && componentLookups.EnemyWeaponLookup.HasComponent(entityA)) // Enemy Hit Player
        {
            DamagePlayer enemyProj = componentLookups.EnemyWeaponLookup.GetRW(entityA).ValueRW;
            PlayerData player = componentLookups.PlayerLookup.GetRW(entityB).ValueRW;
            player.LastDamage += enemyProj.Damage;
            componentLookups.PlayerLookup.GetRW(entityB).ValueRW = player;
            if (enemyProj.DieOnHit)
            {
                destroyedSetWriter.Add(entityA);
            }
        }
        else if (
                componentLookups.EnemyLookup.HasComponent(entityB)
                && componentLookups.PlayerWeaponLookup.HasComponent(entityA)) // Player Hit Enemy
        {
            EnemyStats enemy = componentLookups.EnemyLookup.GetRW(entityB).ValueRW;
            PlayerProjectile playerProj = componentLookups.PlayerWeaponLookup.GetRW(entityA).ValueRW;
            if (!enemy.Invulnerable) enemy.Health -= playerProj.Stats.damage;

            playerProj.Health -= (int)math.ceil(10000f / (1+playerProj.Stats.pierce));

            componentLookups.EnemyLookup.GetRW(entityB).ValueRW = enemy;
            componentLookups.PlayerWeaponLookup.GetRW(entityA).ValueRW = playerProj;
                
            if (playerProj.Health <= 0)
            {
                destroyedSetWriter.Add(entityA);
            }
        }
        else if (
                componentLookups.EnemyWeaponLookup.HasComponent(entityB)
                && componentLookups.PlayerWeaponLookup.HasComponent(entityA))
        {
            DamagePlayer enemyProj = componentLookups.EnemyWeaponLookup.GetRW(entityB).ValueRW;
            PlayerProjectile playerProj = componentLookups.PlayerWeaponLookup.GetRW(entityA).ValueRW;
            if (enemyProj.Mass < 0 || playerProj.InfPierce)
            {
                if (enemyProj.Mass < 0 && playerProj.InfPierce) return;
                if (playerProj.InfPierce) destroyedSetWriter.Add(entityB);
                else
                {
                    playerProj.Health -= (int)math.ceil(-(float)enemyProj.Mass / ((1+playerProj.Stats.pierce)*(1+playerProj.Stats.power)));
                    
                    componentLookups.PlayerWeaponLookup.GetRW(entityA).ValueRW = playerProj;
                    
                    if (playerProj.Health <= 0)
                    {
                        destroyedSetWriter.Add(entityA);
                    }
                }
            }
            else
            {
                while (enemyProj.Mass > 0 && playerProj.Health > 0)
                {
                    enemyProj.Mass--;
                    playerProj.Health -= (int)math.ceil(10000f / ((1+playerProj.Stats.pierce)*(1+playerProj.Stats.power)));
                }
                
                componentLookups.EnemyWeaponLookup.GetRW(entityB).ValueRW = enemyProj;
                componentLookups.PlayerWeaponLookup.GetRW(entityA).ValueRW = playerProj;
                
                if (enemyProj.Mass <= 0)
                {
                    destroyedSetWriter.Add(entityB);
                }
                if (playerProj.Health <= 0)
                {
                    destroyedSetWriter.Add(entityA);
                }
            }
        }
        else if (
                componentLookups.TerrainLookup.HasComponent(entityB)
                && (componentLookups.PlayerLookup.HasComponent(entityA)
                    || componentLookups.EnemyLookup.HasComponent(entityA)))
        {
            // Player-terrain and enemy-terrain collisions
            float3 turnipPenetration;
            TurnipCollisionCheck(result.bodyB, result.bodyA, out turnipPenetration);
            var transform = componentLookups.transform.GetRW(entityA);
            transform.ValueRW.Position += turnipPenetration;
        }
        else if (
                componentLookups.TerrainLookup.HasComponent(entityB)
                && (
                    (
                     !componentLookups.EnemyLookup.HasComponent(entityA)
                     && componentLookups.EnemyWeaponLookup.HasComponent(entityA))
                    || componentLookups.PlayerWeaponLookup.HasComponent(entityA)))
        {
            // weapon-terrain collisions
            float3 turnipPenetration;
            TurnipCollisionCheck(result.bodyB, result.bodyA, out turnipPenetration);
            if (any(turnipPenetration)) {
                destroyedSetWriter.Add(entityA);
            }
        }
        else if (
                componentLookups.PlayerLookup.HasComponent(entityA)
                && componentLookups.IntelLookup.HasComponent(entityB))
        {
            PlayerData player = componentLookups.PlayerLookup.GetRW(entityA).ValueRW;
            Intel intel = componentLookups.IntelLookup.GetRW(entityB).ValueRW;
            player.LastIntel += intel.BaseValue;
            componentLookups.PlayerLookup.GetRW(entityA).ValueRW = player;
            destroyedSetWriter.Add(entityB);
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
    ComponentLookup<PostTransformMatrix> postTransformLookup;

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
            postTransformLookup = postTransformLookup,
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
            TerrainLookup = state.GetComponentLookup<Turnips.TurnipTag>(),
            IntelLookup = state.GetComponentLookup<Intel>()
        };

        // Top-down 2D projection
        projectDirection2D = new float3(0f, 1f, 0f);

        physicsObjectQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass, LocalToWorld>()
            .Build(ref state);

        playerQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<PlayerData>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass, LocalToWorld>()
            .Build(ref state);

        playerWeaponQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<PlayerProjectile>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass, LocalToWorld>()
            .Build(ref state);

        enemyQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<EnemyStats>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass, LocalToWorld>()
            .Build(ref state);

        enemyWeaponQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<DamagePlayer>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass, LocalToWorld>()
            .Build(ref state);

        terrainQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<Turnips.TurnipTag, Collider, LocalTransform>()
            .WithAll<PostTransformMatrix, LocalToWorld>()
            .Build(ref state);

        intelQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAllRW<Intel>()
            .WithAllRW<Unity.Physics.PhysicsVelocity, LocalTransform>()
            .WithAll<Collider, Unity.Physics.PhysicsMass, LocalToWorld>()
            .Build(ref state);

        postTransformLookup = state.GetComponentLookup<PostTransformMatrix>();
        

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
        postTransformLookup.Update(ref state);
        var playerEntity = SystemAPI.GetSingletonEntity<PlayerData>();
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEntity);
        var worldBoundHalfSize = new float3(200);
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
        var destroyedSet = new NativeParallelHashSet<Entity>(physicsState.ValueRO.collisionLayer.colliderBodies.Length, Allocator.TempJob);
        var destroyedSetWriter = destroyedSet.AsParallelWriter();

        var pairsProcessor = new PairsProcessor {
            componentLookups = componentLookups,
            destroyedSetWriter = destroyedSetWriter
        };

        var Dependency = buildLayers;

        // Collider player with intel
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerLayer,
                physicsState.ValueRO.intelLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide enemies with enemies
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.enemyLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide enemy weapons with player
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.enemyWeaponLayer,
                physicsState.ValueRO.playerLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide player weapons with enemies
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerWeaponLayer,
                physicsState.ValueRO.enemyLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        // Collide player weapons with enemy weapons
        Dependency = Physics.FindPairs(
                physicsState.ValueRO.playerWeaponLayer,
                physicsState.ValueRO.enemyWeaponLayer, pairsProcessor)
            .ScheduleParallel(Dependency);
        if (DimensionManager.burstDim.Data == Dimension.Three) {
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
            // Collide player weapons with terrain
            Dependency = Physics.FindPairs(
                    physicsState.ValueRO.playerWeaponLayer,
                    physicsState.ValueRO.terrainLayer, pairsProcessor)
                .ScheduleParallel(Dependency);
            // Collide enemy weapons with terrain
            Dependency = Physics.FindPairs(
                    physicsState.ValueRO.enemyWeaponLayer,
                    physicsState.ValueRO.terrainLayer, pairsProcessor)
                .ScheduleParallel(Dependency);
        }

        JobHandle.ScheduleBatchedJobs();
        Dependency.Complete();

        foreach (Entity entity in destroyedSet) {
            state.EntityManager.DestroyEntity(entity);
        }
        destroyedSet.Dispose();

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
}
