
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public partial struct PlayerShootingSystem : ISystem
{
    private ComponentLookup<LocalTransform> _localTransformLookup;
    private NativeArray<Entity> _projectiles;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerData>();
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
    }

    public void OnDestroy(ref SystemState state) { }
    
    [BurstCompile]
    public partial struct BulletFiringJob : IJobParallelFor
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public NativeArray<BulletEntity> BulletsToFire;
        public LocalTransform PlayerTransform;
        public quaternion PlayerLookRotation;
    
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public void Execute(int index)
        {
            var be = BulletsToFire[index];
            var stats = be.Stats;
            var bullet = be.Bullet;
            var prefab = be.Prefab;

            var originalTransform = TransformLookup[prefab];
        
            var position = PlayerTransform.TransformPoint(bullet.position);
            var rotation = math.mul(PlayerLookRotation, bullet.rotation);

            Entity newEntity = ECB.Instantiate(index, prefab);
        
            ECB.SetComponent(index, newEntity, LocalTransform.FromPositionRotationScale(
                position,
                rotation,
                originalTransform.Scale
            ));
            ECB.AddComponent(index, newEntity, new PostTransformMatrix{Value = float4x4.Scale(be.Stats.Scale)});
             
            if (stats.Effects.IsLaser)
            {
                var laser = stats.Effects.Laser.Value;
                ECB.AddComponent(index, newEntity, new LaserTag());
                ECB.AddComponent(index, newEntity, new Lifetime { Time = float.MaxValue});
            } 
            else 
            {
                if (stats.Effects.IsCannon)
                {
                    var cannon = stats.Effects.Cannon.Value;
                    ECB.AddComponent(index, newEntity, new BombTag());
                    ECB.AddComponent(index, newEntity, new GunExplosion { Radius = stats.Scale.x * 30, ExplosionMultiplier = 1});
                    if (cannon.Acceleration != 0) ECB.AddComponent(index, newEntity, new CannonAcceleration{Acceleration = cannon.Acceleration});
                    if (cannon.ClusterCount != 0) ECB.AddComponent(index, newEntity, new CannonCluster{ClusterCount = cannon.ClusterCount});
                    if (cannon.RocketCount != 0) ECB.AddComponent(index, newEntity, new CannonRockets{RocketCount = cannon.RocketCount});
                    if (cannon.ShrapnelCount != 0) { ECB.AddComponent(index, newEntity, new CannonShrapnel{ShrapnelCount = cannon.ShrapnelCount}); }
                }
                // Add PhysicsVelocity for regular bullets
                ECB.AddComponent(index, newEntity, new PhysicsVelocity
                {
                    Linear = math.mul(rotation, math.forward()) * stats.Speed
                });
                ECB.AddComponent(index, newEntity, new Lifetime { Time = stats.Stats.duration });
            }
            ECB.AddComponent(index, newEntity, new PlayerProjectileDeath
            {
                Stats = stats.Stats,
                Position = position,
            });
            ECB.AddComponent(index, newEntity, new PlayerProjectile()
            {
                Health = 10000,
            });
        }
    }

    public struct BulletEntity
    {
        public Bullet Bullet;
        public AttackInfo Stats;
        public Entity Prefab;
    }

    
    public void OnUpdate(ref SystemState state)
    {
        _localTransformLookup.Update(ref state);
        
        var deltaTime = SystemAPI.Time.DeltaTime;
        var bullets = PlayerManager.Bullets;
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        Entity player = SystemAPI.GetSingletonEntity<PlayerAspect>();
        PlayerAspect p = SystemAPI.GetAspect<PlayerAspect>(player);
        var playerVelocity = SystemAPI.GetComponent<PhysicsVelocity>(player);
        var projectilePrefabs = SystemAPI.GetBuffer<PlayerProjectilePrefab>(player);
        
        PlayerData pData = p.Player;
        PlayerManager.main.DoDamage(pData.LastDamage);
        pData.LastDamage = 0;
        PlayerManager.main.inventory.AddIntel(pData.LastIntel);
        pData.LastIntel = 0;
        
        var bulletsToFire = new List<BulletEntity>();

        if (!_projectiles.IsCreated)
        {
            _projectiles = new NativeArray<Entity>(projectilePrefabs.Length, Allocator.Persistent);
            for (int i = 0; i < projectilePrefabs.Length; i++)
            {
                _projectiles[i] = projectilePrefabs[i].Projectile;
            }
        }
        
        foreach (var attack in bullets)
        {
            var bulletQueue = attack.Bullets;
            
            //var prefab = projectiles.ElementAt((int)attack.projectile).Projectile;

            while (bulletQueue.Count > 0 && SystemAPI.Time.ElapsedTime > bulletQueue.Peek().time)
            {
                var bulletData = bulletQueue.Dequeue();
                var prefab = _projectiles[(int)attack.Projectile];
                bulletsToFire.Add(new() {
                    Bullet = bulletData,
                    Stats = attack.Info,
                    Prefab = prefab
                });
            }
        }

        bullets.RemoveAll(a => a.Bullets.Count == 0);
        
        var job = new BulletFiringJob
        {
            ECB = ecb,
            BulletsToFire = new NativeArray<BulletEntity>(bulletsToFire.ToArray(), Allocator.TempJob),
            PlayerTransform = LocalTransform.FromMatrix(PlayerManager.main.transform.localToWorldMatrix),
            PlayerLookRotation = PlayerManager.main.movement.LookRotation,
            TransformLookup = _localTransformLookup,
        };

        // Schedule with initial dependency chain
        JobHandle handle = job.Schedule(bulletsToFire.Count, 64, state.Dependency);
        
        
        // Combine disposals with main handle
        //handle = job.PlayerTransform.Dispose(handle);

        state.Dependency = handle;
    
        p.Player = pData;
        
    }
    
    
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}