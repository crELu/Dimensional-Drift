
using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Collider = Latios.Psyshock.Collider;
using SphereCollider = Latios.Psyshock.SphereCollider;

[BurstCompile]
public partial struct CannonExplosionSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerData>();
        state.RequireForUpdate<PhysicsSystemState>();
        state.RequireForUpdate<ExplosionPoint>();
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, explosionPoint) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<ExplosionPoint>>())
        {
            explosionPoint.ValueRW.Position = transform.ValueRO.Position;
        }

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (explosionPoint, entity) in SystemAPI.Query<RefRO<ExplosionPoint>>().WithNone<LocalTransform>().WithEntityAccess())
        {
            RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
            var exp = explosionPoint.ValueRO;
            physicsState.ValueRO.GetInRadius(exp.Position, exp.Radius, physicsState.ValueRO.EnemyLayer, out BodiesInRadius enemyInRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in enemyInRadius) {
                
                var enemyPos = SystemAPI.GetComponent<EnemyCollisionReceiver>(result.Item1.entity);
                if (!enemyPos.Invulnerable) enemyPos.LastDamage += exp.Stats.damage;
                SystemAPI.SetComponent(result.Item1.entity, enemyPos);
            }
            physicsState.ValueRO.GetInRadius(exp.Position, exp.Radius, physicsState.ValueRO.EnemyGhostLayer, out BodiesInRadius enemyGhostInRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in enemyGhostInRadius) {
                
                var enemyPos = SystemAPI.GetComponent<EnemyCollisionReceiver>(result.Item1.entity);
                if (!enemyPos.Invulnerable) enemyPos.LastDamage += exp.Stats.damage;
                SystemAPI.SetComponent(result.Item1.entity, enemyPos);
            }
            physicsState.ValueRO.GetInRadius(exp.Position, exp.Radius, physicsState.ValueRO.EnemyWeaponLayer, out BodiesInRadius projInRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in projInRadius) {
                var enemyProj = SystemAPI.GetComponent<DamagePlayer>(result.Item1.entity);
                SystemAPI.SetComponent(result.Item1.entity, enemyProj);
                if (enemyProj.Mass != -1)
                {
                    ecb.DestroyEntity(result.Item1.entity);
                }
            }
            ecb.RemoveComponent<ExplosionPoint>(entity);
        }
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}