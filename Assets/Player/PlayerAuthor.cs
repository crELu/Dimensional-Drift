using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public class PlayerAuthor : BaseAuthor
{
    public List<GameObject> projectiles;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerData
        {
            Health = 100,
            Shield = 100
        });
        var buffer = baker.AddBuffer<PlayerProjectilePrefab>(entity);
        for (int i = 0; i < projectiles.Count; i++)
        {
            buffer.Add(new PlayerProjectilePrefab
            {
                Projectile = baker.ToEntity(projectiles[i])
            });
        }
        base.Bake(baker, entity);
    }
}

[InternalBufferCapacity(10)]
public struct PlayerProjectilePrefab: IBufferElementData
{
    public Entity Projectile;
}

public struct PlayerData : IComponentData
{
    public int Health;
    public int Shield;
    public float AttackTime;
}

public readonly partial struct PlayerAspect : IAspect
{
    public readonly Entity Self;
    
    private readonly RefRW<PlayerData> _player;
    private readonly RefRW<LocalTransform> _localTransform;
    private readonly RefRW<PhysicsVelocity> _physicsVelocity;
    private readonly RefRO<PhysicsMass> _physicsMass;

    public PlayerData Player {
        get => _player.ValueRW;
        set => _player.ValueRW = value;
    }
    public LocalTransform Transform {
        get => _localTransform.ValueRW;
        set => _localTransform.ValueRW = value;
    }
    public PhysicsVelocity PhysicsVelocity {
        get => _physicsVelocity.ValueRW;
        set => _physicsVelocity.ValueRW = value;
    }

    public PhysicsMass PhysicsMass => _physicsMass.ValueRO;

}

public partial struct PlayerSystem : ISystem
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
        public float3 PlayerPosition;
    
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;

        public void Execute(int index)
        {
            var be = BulletsToFire[index];
            var bullet = be.Bullet;
            var prefab = be.Prefab;

            var originalTransform = TransformLookup[prefab];
        
            var position = PlayerPosition + math.mul(PlayerTransform.Rotation, bullet.position);
            var rotation = math.mul(PlayerLookRotation, bullet.rotation);

            Entity newEntity = ECB.Instantiate(index, prefab);
        
            ECB.SetComponent(index, newEntity, LocalTransform.FromPositionRotationScale(
                position,
                rotation,
                originalTransform.Scale
            ));
        
            ECB.AddComponent(index, newEntity, new Lifetime { Time = be.Stats.duration });
            ECB.AddComponent(index, newEntity, new PhysicsVelocity
            {
                Linear = math.mul(rotation, math.forward()) * be.Stats.speed
            });
            ECB.AddComponent(index, newEntity, new PlayerProjectile()
            {
                Damage = be.Stats.damage
            });
        }
    }

    public struct BulletEntity
    {
        public Bullet Bullet;
        public BulletStats Stats;
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
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(player);
        var projectilePrefabs = SystemAPI.GetBuffer<PlayerProjectilePrefab>(player);
        
        PlayerData pData = p.Player;

        if (PlayerManager.fire)
        {
            pData.AttackTime = 0;
        }
        pData.AttackTime += deltaTime;
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

            while (bulletQueue.Count > 0 && pData.AttackTime > bulletQueue.Peek().time)
            {
                var bulletData = bulletQueue.Dequeue();
                var prefab = _projectiles[(int)attack.projectile];
                bulletsToFire.Add(new() {
                    Bullet = bulletData,
                    Stats = attack.bulletStats,
                    Prefab = prefab
                });
                
                //Entity newEntity = ecb.Instantiate(prefab);
                
                // var originalTransform = _localTransformLookup[prefab];
                //
                // var position = p.Transform.TransformPoint(bulletData.position);
                // var rotation = math.mul(PlayerManager.main.LookRotation, bulletData.rotation);
                //
                // ecb.SetComponent(newEntity, LocalTransform.FromPositionRotationScale(
                //     position,
                //     rotation,
                //     originalTransform.Scale
                // ));
                // ecb.SetComponent(newEntity, new Lifetime(){Time = attack.lifetime});
                // var v = new PhysicsVelocity { Linear = math.mul(rotation, math.forward()) * attack.speed };
                // ecb.AddComponent(newEntity, v);
            }
        }
        
        var job = new BulletFiringJob
        {
            ECB = ecb,
            BulletsToFire = new NativeArray<BulletEntity>(bulletsToFire.ToArray(), Allocator.TempJob),
            PlayerTransform = playerTransform,
            PlayerLookRotation = PlayerManager.main.movement.LookRotation,
            PlayerPosition = playerTransform.Position,
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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
public partial struct PlayerPhysicsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // state.RequireForUpdate<PlayerAspect>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        foreach (var player in SystemAPI.Query<PlayerAspect>())
        {
            var dt = SystemAPI.Time.fixedDeltaTime;
            var playerData = PlayerManager.main;
            var transform = player.Transform;
            var playerPhysics = player.PhysicsVelocity;
            var impulse = playerData.movement.GetMovement(player.PhysicsVelocity.Linear) * dt + playerData.movement.GetDash();
            

            playerPhysics.ApplyImpulse(player.PhysicsMass, transform.Position, transform.Rotation, impulse, transform.Position);
            //playerPhysics.ApplyAngularImpulse(player.PhysicsMass, playerData.GetRotation(transform.Rotation) * dt);
            playerPhysics.Angular = float3.zero;
            playerData.movement.Position = player.Transform.Position;
            transform.Rotation = playerData.transform.rotation;
            
            player.Transform = transform;
            player.PhysicsVelocity = playerPhysics;
        }
    }
}