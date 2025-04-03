using System.Collections.Generic;
using Enemies.Systems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public class EnemyShootingAuthor : BaseAuthor
{
    [Header("Base Settings")]
    public bool active = true;
    public GameObject projectile;
    public float range;
    public float speed;
    public Vector2 visionCone;
    public float cd = 1; 
    
    [Header("Burst Settings")]
    public int bursts = 1;
    public float burstLength = .5f;
     
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        var guns = GetComponentsInChildren<EnemyGunTag>();
        var buffer = baker.AddBuffer<EnemyShoot>(entity);
        buffer.Capacity = guns.Length;
        var builder = new BlobBuilder(Allocator.Temp);
            
        ref ShootingArrayBlob arrayBlob = ref builder.ConstructRoot<ShootingArrayBlob>();
        
        BlobBuilderArray<ShootingBlob> singlesBuilder = builder.Allocate(
            ref arrayBlob.Guns,
            guns.Length
        );
        
        for (int i = 0; i < guns.Length; i++)
        {
            var data = guns[i];
            singlesBuilder[i] = new ShootingBlob
            {
                Position = guns[i].transform.localPosition,
                Forward = guns[i].transform.localRotation * Vector3.forward,
                Up = guns[i].transform.localRotation * Vector3.up,
                Range = range,
                Speed = speed,
                VisionConeMinMax = visionCone,
                MaxCooldown = cd,
                BurstCount = bursts,
                BurstLength = burstLength,
                SpreadAngle = data.spreadAngle,
                SpreadCount = data.spreadCount,
                Distance = data.distance,
            };
            buffer.Add(new EnemyShoot
            {
                Projectile = baker.ToEntity(projectile),
                Active = active,
            });
        }
        var linearBlob = builder.CreateBlobAssetReference<ShootingArrayBlob>(Allocator.Persistent);
        builder.Dispose();
        baker.AddBlobAsset(ref linearBlob, out var hash1);
        baker.AddComponent(entity, new ShootingData{Data = linearBlob});
        
        base.Bake(baker, entity);
    }
}

public struct ShootingBlob
{
    public float3 Position;
    public float3 Forward;
    public float3 Up;
    public float Range;
    public float Speed;
    public float2 VisionConeMinMax; // x = min angle, y = max angle (degrees)
    public float MaxCooldown;
    public int SpreadCount;
    public float SpreadAngle;
    public float Distance;
    public int BurstCount; // Number of shots in a burst
    public float BurstLength; // Time between shots in a burst
}

public struct ShootingArrayBlob
{
    public BlobArray<ShootingBlob> Guns;
}

public struct ShootingData : IComponentData
{
    public BlobAssetReference<ShootingArrayBlob> Data;
}

public struct EnemyShoot : IBufferElementData
{
    public Entity Projectile;
    public bool Active;
    public float CurrentCooldown;
    public int ShotsFired; // Number of shots fired in the current burst
    public float CurrentBurstTimer; // Timer between burst shots
}

[BurstCompile]
public partial struct EnemyShootingSystem : ISystem
{
    private EntityQuery _playerQuery;
    private ComponentLookup<LocalTransform> _transformLookup;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerData>());
        _transformLookup = state.GetComponentLookup<LocalTransform>();
        state.RequireForUpdate<PlayerData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (_playerQuery.IsEmpty) return;
        _transformLookup.Update(ref state);
        
        // Get player position
        var player = SystemAPI.GetSingletonEntity<PlayerData>();
        var playerPos = SystemAPI.GetComponent<LocalTransform>(player).Position;
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // Shooting job
        state.Dependency = new ProcessShootingJob
        {
            PlayerPosition = playerPos,
            DeltaTime = deltaTime,
            ECB = ecb,
            Dim = DimensionManager.burstDim.Data,
            Transform = _transformLookup,
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    partial struct ProcessShootingJob : IJobEntity
    {
        [ReadOnly] public float3 PlayerPosition;
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        public Dimension Dim;
        [ReadOnly] public ComponentLookup<LocalTransform> Transform;
        
        void Execute([ChunkIndexInQuery] int index, Entity entity, in ShootingData data, ref DynamicBuffer<EnemyShoot> shoots)
        {
            var transform = Transform[entity];
            for (int i = 0; i < shoots.Length; i++)
            {
                var shoot = shoots[i];
                ref var stats = ref data.Data.Value.Guns;
                // Update cooldown and burst timer
                shoot.CurrentCooldown = math.max(shoot.CurrentCooldown - DeltaTime, 0f);
                shoot.CurrentBurstTimer = math.max(shoot.CurrentBurstTimer - DeltaTime, 0f);

                // Skip if inactive
                if (!shoot.Active)
                {
                    shoots[i] = shoot;
                    continue;
                }

                // If in a burst, wait for the burst delay
                if (shoot.ShotsFired > 0 && shoot.ShotsFired < stats[i].BurstCount)
                {
                    if (shoot.CurrentBurstTimer > 0)
                    {
                        shoots[i] = shoot;
                        continue;
                    }
                }
                // If no burst shots remain, wait for the full cooldown
                else if (shoot.CurrentCooldown > 0)
                {
                    shoots[i] = shoot;
                    continue;
                }

                // Calculate to-player vector
                float3 toPlayer = PlayerPosition - transform.Position;
                if (Dim == Dimension.Two) toPlayer.y = 0;
                float distance = math.length(toPlayer);
                float3 forward = transform.TransformDirection(stats[i].Forward);
                
                // Range check
                if (distance > stats[i].Range)
                {
                    shoots[i] = shoot;
                    continue;
                }

                if (shoot.ShotsFired == 0)
                {
                    // Vision cone check
                    float3 dirToPlayer = math.normalize(toPlayer);
                    float angle = math.degrees(math.acos(math.dot(forward, dirToPlayer)));
                    
                    if (angle < stats[i].VisionConeMinMax.x || angle > stats[i].VisionConeMinMax.y)
                    {
                        shoots[i] = shoot;
                        continue;
                    }
                }
                
                float3 up = transform.TransformDirection(stats[i].Up);
                quaternion baseRot = quaternion.LookRotation(forward, up);
                float3 pos = transform.TransformPoint(stats[i].Position);
                var t = Transform[shoot.Projectile];
                
                float step = stats[i].SpreadCount > 1 ? stats[i].SpreadAngle / (stats[i].SpreadCount - 1) : 0f;
                float start = stats[i].SpreadCount > 1 ? -stats[i].SpreadAngle / 2f : 0f;
                
                for (int j = 0; j < stats[i].SpreadCount; j++)
                {
                    quaternion rot = math.mul(baseRot, quaternion.EulerZXY(0, math.TORADIANS * (start + j * step), 0));
                    float3 dir = math.mul(rot, math.forward());
                    // Fire projectile
                    Entity projectile = ECB.Instantiate(index, shoot.Projectile);
                    ECB.SetComponent(index, projectile, new LocalTransform
                    {
                        Position = pos + dir * stats[i].Distance,
                        Rotation = rot,
                        Scale = t.Scale
                    });
                    ECB.AddComponent(index, projectile, new PhysicsVelocity { Linear = dir * stats[i].Speed });
                }

                // Update burst counters
                shoot.ShotsFired++;
                shoot.CurrentBurstTimer = stats[i].BurstLength / stats[i].BurstCount;

                // Reset burst if all shots fired
                if (shoot.ShotsFired >= stats[i].BurstCount)
                {
                    shoot.ShotsFired = 0;
                    shoot.CurrentCooldown = stats[i].MaxCooldown;
                }

                shoots[i] = shoot;
            }
        }
    }
}