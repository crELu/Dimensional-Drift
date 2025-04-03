using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct BaseEffects
{
    public float HomingSpeed;
    public float Radius;
    public float ExplosionMultiplier;
    public bool IsLaser => Laser.HasValue;
    public bool IsCannon => Cannon.HasValue;
    public LaserEffects? Laser;
    public CannonEffects? Cannon;
}

public struct GunHoming : IComponentData
{
    public float HomingSpeed;
}

public struct GunExplosion : ICleanupComponentData
{
    public float Radius;
    public float ExplosionMultiplier;
}

public struct PlayerProjectileDeath : ICleanupComponentData
{
    public float3 Position;
    public BulletStats Stats;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
//[BurstCompile]
public partial struct GunSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerData>();
    }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, proj) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerProjectileDeath>>())
        {
            proj.ValueRW.Position = transform.ValueRO.Position;
        }
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        foreach (var (death, explosionPoint, entity) in SystemAPI.Query<RefRO<PlayerProjectileDeath>, RefRO<GunExplosion>>().WithNone<LocalTransform>().WithEntityAccess())
        {
            RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
            var exp = explosionPoint.ValueRO;
            var proj = death.ValueRO;
            physicsState.ValueRO.GetInRadius(proj.Position, exp.Radius, physicsState.ValueRO.EnemyLayer, out BodiesInRadius enemyInRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in enemyInRadius)
            {
                if (!state.EntityManager.Exists(result.Item1.entity)) continue;
                var enemyPos = SystemAPI.GetComponent<EnemyCollisionReceiver>(result.Item1.entity);
                if (!enemyPos.Invulnerable) enemyPos.LastDamage += proj.Stats.damage * exp.ExplosionMultiplier;
                SystemAPI.SetComponent(result.Item1.entity, enemyPos);
                Debug.Log($"{proj.Stats.damage}");
            }
            physicsState.ValueRO.GetInRadius(proj.Position, exp.Radius, physicsState.ValueRO.EnemyGhostLayer, out BodiesInRadius enemyGhostInRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in enemyGhostInRadius)
            {
                if (!state.EntityManager.Exists(result.Item1.entity)) continue;
                var enemyPos = SystemAPI.GetComponent<EnemyCollisionReceiver>(result.Item1.entity);
                if (!enemyPos.Invulnerable) enemyPos.LastDamage += proj.Stats.damage * exp.ExplosionMultiplier;
                SystemAPI.SetComponent(result.Item1.entity, enemyPos);
            }
            physicsState.ValueRO.GetInRadius(proj.Position, exp.Radius, physicsState.ValueRO.EnemyWeaponLayer, out BodiesInRadius projInRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in projInRadius)
            {
                if (!state.EntityManager.Exists(result.Item1.entity)) continue;
                var enemyProj = SystemAPI.GetComponent<DamagePlayer>(result.Item1.entity);
                SystemAPI.SetComponent(result.Item1.entity, enemyProj);
                if (enemyProj.Mass != -1)
                {
                    ecb.DestroyEntity(result.Item1.entity);
                }
            }
            ecb.RemoveComponent<GunExplosion>(entity);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        
        var dying = SystemAPI.QueryBuilder().WithAll<PlayerProjectileDeath>().WithNone<LocalTransform>().Build();
        state.EntityManager.RemoveComponent<PlayerProjectileDeath>(dying);
    }
}