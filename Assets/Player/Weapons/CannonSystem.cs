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

public struct CannonEffects
{
    public int ShrapnelCount;
    public int RocketCount;
    public int ClusterCount;
    public float Acceleration;
}

public struct CannonShrapnel : ICleanupComponentData
{
    public int ShrapnelCount;
}

public struct CannonRockets : ICleanupComponentData
{
    public int RocketCount;
}

public struct CannonCluster : ICleanupComponentData
{
    public int ClusterCount;
}

public struct CannonAcceleration : IComponentData
{
    public float Acceleration;
}

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(GunSystem))]
public partial struct CannonSystem : ISystem
{
    private Rng _rng;
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerData>();
        state.RequireForUpdate<PhysicsSystemState>();
        _rng = new Rng("CannonSystem");
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var player = SystemAPI.GetSingletonEntity<PlayerData>();
        var projectiles = state.EntityManager.GetBuffer<PlayerProjectilePrefab>(player);
        foreach (var (death, data, entity) in 
                 SystemAPI.Query<RefRO<PlayerProjectileDeath>, RefRO<CannonShrapnel>>()
                     .WithNone<LocalTransform>().WithEntityAccess())
        {
            var shrap = projectiles[(int)Attack.ProjectileType.ChargeShrapnel].Projectile;
            var stats = death.ValueRO;
            stats.Stats.damage *= .8f;
            Instantiate(ecb, state.EntityManager, stats, shrap, data.ValueRO.ShrapnelCount, 70, 10, 0);
            ecb.RemoveComponent<CannonShrapnel>(entity);
        }
        foreach (var (death, data, entity) in 
                 SystemAPI.Query<RefRO<PlayerProjectileDeath>, RefRO<CannonRockets>>()
                     .WithNone<LocalTransform>().WithEntityAccess())
        {
            var rocket = projectiles[(int)Attack.ProjectileType.ChargeRockets].Projectile;
            var stats = death.ValueRO;
            stats.Stats.damage *= .5f;
            Instantiate(ecb, state.EntityManager, stats, rocket,data.ValueRO.RocketCount, 60, 5, 10);
            ecb.RemoveComponent<CannonRockets>(entity);
        }
        foreach (var (death, data, entity) in 
                 SystemAPI.Query<RefRO<PlayerProjectileDeath>, RefRO<CannonCluster>>()
                     .WithNone<LocalTransform>().WithEntityAccess())
        {
            var cluster = projectiles[(int)Attack.ProjectileType.ChargeRecursive].Projectile;
            var stats = death.ValueRO;
            stats.Stats.damage *= .33f;
            Instantiate(ecb, state.EntityManager, stats, cluster,data.ValueRO.ClusterCount, 40, 10, 20);
            ecb.RemoveComponent<CannonCluster>(entity);
        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }

    [BurstCompile]
    public void Instantiate(EntityCommandBuffer ecb, EntityManager mgr, PlayerProjectileDeath parent, Entity prefab, int r, int speed, float spacing, float expRadius)
    {
        var transform = mgr.GetComponentData<LocalTransform>(prefab);
        var rand = _rng.Shuffle().GetSequence(1);
        for (int i = 0; i < r; i++)
        {
            var newEntity = ecb.Instantiate(prefab);
            
            float phi = (1 + math.sqrt(5)) / 2; // Golden ratio

            float z = 1f - (2f * i) / (r - 1);  // Map index to [-1,1]
            float radius = math.sqrt(1 - z * z); // Compute radius at height z
            float theta = 2f * math.PI * i / phi; // Angle offset by golden ratio

            float x = radius * math.cos(theta);
            float y = radius * math.sin(theta);
            var pos = new float3(x, y, z);
            var extraRot = rand.NextQuaternionRotation();
            var rot = quaternion.LookRotation(pos, Mathf.Approximately(pos.y, 1) ? math.right() : math.up());
            ecb.AddComponent(newEntity, new PlayerProjectile
            {
                Health = 10000,
            });
            ecb.AddComponent(newEntity, new PhysicsVelocity
            {
                Linear = math.mul(extraRot, new float3(x, y, z)) * speed
            });
            if (expRadius != 0)
            {
                ecb.AddComponent(newEntity, new GunExplosion
                {
                    ExplosionMultiplier = 1,
                    Radius = expRadius,
                });
            }
            ecb.AddComponent(newEntity, LocalTransform.FromPositionRotationScale(parent.Position + pos * spacing, math.mul(extraRot, rot), transform.Scale));
            ecb.AddComponent(newEntity, parent);
        }
    }
}

