using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using UnityEngine;
using Physics = Latios.Psyshock.Physics;

[BurstCompile]
public struct TurnipPairs: IFindPairsProcessor {
    public PhysicsComponentLookups ComponentLookups;
    public NativeParallelHashSet<Entity>.ParallelWriter DestroyedSetWriter;
    
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

    [BurstCompile]
    private void StalkCollisionCheck(
            float turnipRadius,
            in float3 turnipRelativePosition,
            float bodyRadius,
            float maxStalkSize,
            float minStalkSize,
            float sign,
            out float3 penetration)
    {
        penetration = new float3(0);

        float yPosition = sign * turnipRelativePosition.y;
        if (yPosition < 0.0f) {
            return; // return early
        }

        float turnipStalkRadius = max(
                lerp(maxStalkSize, minStalkSize, max(yPosition + bodyRadius, 0.0f) / turnipRadius),
                lerp(maxStalkSize, minStalkSize, max(yPosition - bodyRadius, 0.0f) / turnipRadius));
        // Minimum distance the body can be from the turnip
        // without intersecting
        float minStalkDistance = bodyRadius + turnipStalkRadius;
        float3 diff = turnipRelativePosition;
        diff.y = 0;
        if (length(diff) < minStalkDistance) {
            penetration += normalize(diff) * (minStalkDistance - length(diff));
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
        float widthRatio = 0.6f;
        // Ratio between height of "body" of turnip and total height of turnip
        float heightRatio = 0.38f;
        // Ratio between radius of "body" of turnip and total height of turnip
        float radiusRation = 0.5f;
        // Ratio between width of "stalk" of turnip in the middle of the body
        // and total height of turnip
        float topStalkMaxRatio = 0.162f;
        float bottomStalkMaxRatio = 0.3f;
        float bottomStalkMinRatio = 0.05f;
        // Ratio between vertical center of turnip "body" and total height of
        // turnip
        float bodyPositionRatio = 0.1f;

        Aabb bodyAabb = Physics.AabbFrom(body.collider, body.transform);
        float bodyRadius = max(
                max(bodyAabb.max.x - bodyAabb.min.x,
                bodyAabb.max.y - bodyAabb.min.y),
                bodyAabb.max.z - bodyAabb.min.z) / 2;

        Aabb turnipAabb = Physics.AabbFrom(turnipBody.collider, turnipBody.transform);
        float turnipRadius = (turnipAabb.max.x - turnipAabb.min.x) / 2;

        float turnipBodyWidth = turnipRadius * widthRatio;
        float turnipBodyHeight = turnipRadius * heightRatio;
        float turnipBodyPosition = bodyPositionRatio * heightRatio;

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
            if (abs(turnipRelativePosition.y - turnipBodyPosition) < minBodyDistance) {
                float3 xzPosition = turnipRelativePosition;
                xzPosition.y = 0.0f;
                float weight = length(xzPosition) / turnipBodyWidth;
                float turnipBodyRadiusSquared = (1 - square((turnipRelativePosition.y - turnipBodyPosition) / turnipBodyHeight)) * square(turnipBodyWidth);
                if (turnipBodyRadiusSquared > 0) {
                    float turnipBodyRadius = sqrt(turnipBodyRadiusSquared);
                    penetration += normalize(xzPosition) * (length(xzPosition) - turnipBodyRadius);
                }

                penetration.y +=
                    normalize(turnipRelativePosition).y
                    * (minBodyDistance - abs(turnipRelativePosition.y));
            }
        }

        float3 topStalkPenetration;
        float3 bottomStalkPenetration;

        // check for intersection with turnip "stalk"
        float topMaxStalkSize = turnipRadius * topStalkMaxRatio;
        float bottomMaxStalkSize = turnipRadius * bottomStalkMaxRatio;
        float bottomMinStalkSize = turnipRadius * bottomStalkMinRatio;

        StalkCollisionCheck(turnipRadius, turnipRelativePosition, bodyRadius, topMaxStalkSize, 0.0f, 1.0f, out topStalkPenetration);
        StalkCollisionCheck(turnipRadius, turnipRelativePosition, bodyRadius, bottomMaxStalkSize, bottomMinStalkSize, -1.0f, out bottomStalkPenetration);

        penetration += topStalkPenetration;
        penetration += bottomStalkPenetration;

        /*
        float turnipStalkRadius = max(
                lerp(stalkMax, stalk, abs(turnipRelativePosition.y + halfBodyRadius) / turnipRadius),
                lerp(stalkMax, stalk, abs(turnipRelativePosition.y - halfBodyRadius) / turnipRadius));
        // Minimum distance the body can be from the turnip
        // without intersecting
        float minStalkDistance = bodyRadius + turnipStalkRadius;
        float3 diff = turnipRelativePosition;
        diff.y = 0;
        if (length(diff) < minStalkDistance) {
            penetration += normalize(diff) * (minStalkDistance - length(diff));
        }
        */
    }

    [BurstCompile]
    private void Calculate(FindPairsResult result)
    {
        SafeEntity entityB = result.entityB;

        // Obstacle obstacle = ComponentLookups.TerrainLookup.GetRW(entityA).ValueRW;
        float3 turnipPenetration;
        TurnipCollisionCheck(result.bodyA, result.bodyB, out turnipPenetration);

        if (any(turnipPenetration)) {
            if (
                    ComponentLookups.EnemyLookup.HasComponent(entityB)
                    || ComponentLookups.PlayerLookup.HasComponent(entityB))
            {
                var transform = ComponentLookups.transform.GetRW(entityB);
                transform.ValueRW.Position += turnipPenetration;
            }
            else if (
                    ComponentLookups.EnemyWeaponLookup.HasComponent(entityB)
                    || ComponentLookups.PlayerWeaponLookup.HasComponent(entityB))
            {
                DestroyedSetWriter.Add(entityB);
            }
        }
    }
}
