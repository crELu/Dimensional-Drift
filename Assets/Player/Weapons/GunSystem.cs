using System.Runtime.CompilerServices;
using Enemies.AI;
using Latios;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public struct BaseEffects
{
    public float HomingSpeed;
    public float HomingRadius;
    public float Radius;
    public float ExplosionMultiplier;
    public float Interference;
    public bool IsLaser => Laser.HasValue;
    public bool IsCannon => Cannon.HasValue;
    public LaserEffects? Laser;
    public CannonEffects? Cannon;
}

public struct GunExplosion : ICleanupComponentData
{
    public float Radius;
    public float ExplosionMultiplier;
}

public struct GunInterference : IComponentData
{
    public float Strength;
}

public struct PlayerProjectileDeath : ICleanupComponentData
{
    public float3 Position;
    public BulletStats Stats;
}

public struct PlayerProjectileEffectLookups
{
    public PhysicsComponentLookup<GunInterference> InterferenceLookup;
    public PhysicsComponentLookup<LaserRefract> RefractionLookup;
    //public PhysicsComponentLookup<GunInterference> InterferenceLookup;
    public void Init(ref SystemState state)
    {
        InterferenceLookup = state.GetComponentLookup<GunInterference>();
        RefractionLookup = state.GetComponentLookup<LaserRefract>();
    }
    public void Update(ref SystemState state) {
        InterferenceLookup.Update(ref state);
        RefractionLookup.Update(ref state);
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[BurstCompile]
public partial struct GunSystem : ISystem
{
    private Rng _rng;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GraphicsReceiver>();
        state.RequireForUpdate<PlayerData>();
        _rng = new Rng("GunSystem");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, proj) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerProjectileDeath>>())
        {
            proj.ValueRW.Position = transform.ValueRO.Position;
        }
        
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        
        var vfxReceiver = SystemAPI.GetSingleton<GraphicsReceiver>();
        var vfxWriter = vfxReceiver.AudioCommands;
        
        foreach (var (death, explosionPoint, entity) in SystemAPI.Query<RefRO<PlayerProjectileDeath>, RefRO<GunExplosion>>().WithNone<LocalTransform>().WithEntityAccess())
        {
            RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
            var exp = explosionPoint.ValueRO;
            var proj = death.ValueRO;
            physicsState.ValueRO.GetInRadius(proj.Position, exp.Radius, physicsState.ValueRO.EnemyLayer, out BodiesInRadius enemyInRadius);
            vfxWriter.Enqueue(new OneShotData {Name = "PlayerWeaponExplosion", Position = proj.Position, Scale = exp.Radius, Angle = 0});
            foreach (var result in enemyInRadius.enumerator)
            {
                ExplodeEnemy(result, ref state, exp, proj);
            }
            physicsState.ValueRO.GetInRadius(proj.Position, exp.Radius, physicsState.ValueRO.EnemyGhostLayer, out BodiesInRadius enemyGhostInRadius);
            foreach (var result in enemyGhostInRadius.enumerator)
            {
                ExplodeEnemy(result, ref state, exp, proj);
            }
            physicsState.ValueRO.GetInRadius(proj.Position, exp.Radius, physicsState.ValueRO.EnemyWeaponLayer, out BodiesInRadius projInRadius);
            foreach (var result in projInRadius.enumerator)
            {
                if (!state.EntityManager.Exists(result.entity)) continue;
                var enemyProj = SystemAPI.GetComponent<DamagePlayer>(result.entity);
                SystemAPI.SetComponent(result.entity, enemyProj);
                if (enemyProj.Mass != -1)
                {
                    ecb.DestroyEntity(result.entity);
                }
            }
            ecb.RemoveComponent<GunExplosion>(entity);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
        
        var dying = SystemAPI.QueryBuilder().WithAll<PlayerProjectileDeath>().WithNone<LocalTransform>().Build();
        state.EntityManager.RemoveComponent<PlayerProjectileDeath>(dying);
    }
    
    [BurstCompile]
    private void ExplodeEnemy(FindObjectsResult objects, ref SystemState state, GunExplosion explosion, PlayerProjectileDeath projectile)
    {
        var rand = _rng.Shuffle().GetSequence(1);
        if (!state.EntityManager.Exists(objects.entity)) return;
        var enemyPos = SystemAPI.GetComponent<EnemyCollisionReceiver>(objects.entity);
        if (!enemyPos.Invulnerable)
        {
            var enemyVel = SystemAPI.GetComponent<PhysicsVelocity>(objects.entity);
            enemyPos.LastDamage += projectile.Stats.damage * explosion.ExplosionMultiplier;
            var force = math.sqrt(explosion.Radius / enemyPos.Size / enemyPos.Size * 10) * 6;
            enemyVel.Angular += rand.NextFloat3(-1, 1) * force;
            enemyVel.Linear += math.normalize(objects.transform.position - projectile.Position) * 5 * force;
            SystemAPI.SetComponent(objects.entity, enemyVel);
        }
        SystemAPI.SetComponent(objects.entity, enemyPos);
        
    }
}