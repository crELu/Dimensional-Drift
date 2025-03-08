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
using UnityEngine.SocialPlatforms;
using Random = UnityEngine.Random;

public class PlayerAuthor : BaseAuthor
{
    public List<GameObject> projectiles;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerData
        {
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
    public float AttackTime;
    public float LastDamage;
    public float LastIntel;
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
    private ComponentLookup<LaserTag> _laserTagLookup;
    private NativeArray<Entity> _projectiles;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerData>();
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        _laserTagLookup = state.GetComponentLookup<LaserTag>(isReadOnly: true);
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
        public PhysicsVelocity PlayerVelocity;
    
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [ReadOnly] public ComponentLookup<LaserTag> LaserTagLookup;

        public void Execute(int index)
        {
            var be = BulletsToFire[index];
            var bullet = be.Bullet;
            var prefab = be.Prefab;

            var originalTransform = TransformLookup[prefab];
        
            var position = bullet.position;
            var rotation = math.mul(PlayerLookRotation, bullet.rotation);

            Entity newEntity = ECB.Instantiate(index, prefab);
        
            ECB.SetComponent(index, newEntity, LocalTransform.FromPositionRotationScale(
                position,
                rotation,
                originalTransform.Scale
            ));
            ECB.AddComponent(index, newEntity, new PostTransformMatrix{Value = float4x4.Scale(be.Stats.Scale)});
            
            
            if (IsLaser(prefab))
            {
                ECB.AddComponent(index, newEntity, new LaserTag());
                ECB.AddComponent(index, newEntity, new Lifetime { Time = float.MaxValue});
            }
            else 
            {
                // Add PhysicsVelocity for regular bullets
                ECB.AddComponent(index, newEntity, new PhysicsVelocity
                {
                    Linear = PlayerVelocity.Linear + math.mul(rotation, math.forward()) * be.Stats.Speed
                });
                ECB.AddComponent(index, newEntity, new Lifetime { Time = be.Stats.Stats.duration });

            }
            ECB.AddComponent(index, newEntity, new PlayerProjectile()
            {
                Stats = be.Stats.Stats,
                Health = 10000,
            });
        }
        
        private bool IsLaser(Entity prefab)
        {
            return LaserTagLookup.HasComponent(prefab);
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
        _laserTagLookup.Update(ref state);
        
        var deltaTime = SystemAPI.Time.DeltaTime;
        var bullets = PlayerManager.Bullets;
        EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);

        Entity player = SystemAPI.GetSingletonEntity<PlayerAspect>();
        PlayerAspect p = SystemAPI.GetAspect<PlayerAspect>(player);
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(player);
        var playerVelocity = SystemAPI.GetComponent<PhysicsVelocity>(player);
        var projectilePrefabs = SystemAPI.GetBuffer<PlayerProjectilePrefab>(player);
        
        PlayerData pData = p.Player;
        PlayerManager.main.DoDamage(pData.LastDamage);
        pData.LastDamage = 0;
        PlayerManager.main.intel += pData.LastIntel;
        pData.LastIntel = 0;
        
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
                var prefab = _projectiles[(int)attack.Projectile];
                bulletsToFire.Add(new() {
                    Bullet = bulletData,
                    Stats = attack.Info,
                    Prefab = prefab
                });
            }
        }
        
        
        var job = new BulletFiringJob
        {
            ECB = ecb,
            BulletsToFire = new NativeArray<BulletEntity>(bulletsToFire.ToArray(), Allocator.TempJob),
            PlayerTransform = playerTransform,
            PlayerLookRotation = PlayerManager.main.movement.LookRotation,
            PlayerPosition = playerTransform.Position,
            PlayerVelocity = playerVelocity,
            TransformLookup = _localTransformLookup,
            LaserTagLookup = _laserTagLookup
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
            
            playerPhysics.Linear += (float3)impulse;
            
            playerPhysics.Angular = float3.zero;
            playerData.movement.Position = player.Transform.Position;
            transform.Rotation = playerData.transform.rotation;
                
            player.Transform = transform;
            player.PhysicsVelocity = playerPhysics;
        }
    }
}

[UpdateInGroup(typeof(PresentationSystemGroup ))]
public partial struct LaserSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        //state.RequireForUpdate<PlayerAspect>();
        state.RequireForUpdate<PlayerData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity playerE = SystemAPI.GetSingletonEntity<PlayerAspect>();
        PlayerAspect player = SystemAPI.GetAspect<PlayerAspect>(playerE);
        var playerTransform = player.Transform;
        var playerData = PlayerManager.main;
        if (LaserWeapon.LaserIsActive)
        {
            foreach (var (laserTransform, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<LaserTag>>())
            {
                laserTransform.ValueRW.Position = playerTransform.Position + 
                                                  math.rotate(playerData.transform.rotation, LaserWeapon.LaserOffset);
                laserTransform.ValueRW.Rotation = playerData.movement.LookRotation;
            }
        }
        else
        {
            DespawnLasers(ref state);
        }
    }
    
    [BurstCompile]
    partial struct DespawnLasersJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
    
        public void Execute(Entity entity, [EntityIndexInQuery] int index, in LaserTag laser)
        {
            ECB.DestroyEntity(index, entity);
        }
    }

    private void DespawnLasers(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new DespawnLasersJob { ECB = ecb.AsParallelWriter() };
    
        state.Dependency = job.ScheduleParallel(state.Dependency);
    
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}