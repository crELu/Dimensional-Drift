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
    public GameObject projectile;
    public float range;
    public float speed;
    public Vector2 visionCone;
    public float cd;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        var guns = GetComponentsInChildren<EnemyGunTag>();
        var buffer = baker.AddBuffer<EnemyShoot>(entity);
        for (int i = 0; i < guns.Length; i++)
        {
            buffer.Add(new EnemyShoot
            {
                Projectile = baker.ToEntity(projectile),
                Active = true,
                Position = guns[i].transform.localPosition,
                Forward = guns[i].transform.forward,
                Range = range,
                Speed = speed,
                VisionConeMinMax = visionCone,
                MaxCooldown = cd
            });
        }
        base.Bake(baker, entity);
    }
}

public struct EnemyShoot : IBufferElementData
{
    public Entity Projectile;
    public bool Active;
    public float3 Position;
    public float3 Forward;
    public float Range;
    public float Speed;
    public float2 VisionConeMinMax; // x = min angle, y = max angle (degrees)
    public float MaxCooldown;
    public float CurrentCooldown;
}

[BurstCompile]
public partial struct EnemyShootingSystem : ISystem
{
    private EntityQuery _playerQuery;
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        _playerQuery = state.GetEntityQuery(ComponentType.ReadOnly<PlayerData>());
        state.RequireForUpdate<PlayerData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (_playerQuery.IsEmpty) return;

        // Get player position
        var player = SystemAPI.GetSingletonEntity<PlayerData>();
        var playerPos = SystemAPI.GetComponent<LocalTransform>(player).Position;
        var deltaTime = SystemAPI.Time.DeltaTime;
        
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // Shooting job
        new ProcessShootingJob
        {
            PlayerPosition = playerPos,
            DeltaTime = deltaTime,
            ECB = ecb
        }.ScheduleParallel();
    }

    [BurstCompile]
    partial struct ProcessShootingJob : IJobEntity
    {
        [ReadOnly] public float3 PlayerPosition;
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
        
        void Execute([EntityIndexInQuery] int index, ref DynamicBuffer<EnemyShoot> shoots, in LocalTransform transform)
        {
            for (int i = 0; i < shoots.Length; i++)
            {
                var shoot = shoots[i];
                
                // Update cooldown
                shoot.CurrentCooldown = math.max(shoot.CurrentCooldown - DeltaTime, 0f);
                
                // Skip if inactive or cooling down
                if (!shoot.Active || shoot.CurrentCooldown > 0)
                {
                    shoots[i] = shoot;
                    continue;
                }

                // Calculate to-player vector
                float3 toPlayer = PlayerPosition - transform.Position;
                float distance = math.length(toPlayer);
                
                // Range check
                if (distance > shoot.Range)
                {
                    shoots[i] = shoot;
                    continue;
                }

                // Vision cone check
                float3 dirToPlayer = math.normalize(toPlayer);
                float angle = math.degrees(math.acos(math.dot(shoot.Forward, dirToPlayer)));
                
                if (angle < shoot.VisionConeMinMax.x || angle > shoot.VisionConeMinMax.y)
                {
                    shoots[i] = shoot;
                    continue;
                }

                // Fire projectile
                Entity projectile = ECB.Instantiate(index, shoot.Projectile);
                ECB.SetComponent(index, projectile, new LocalTransform
                {
                    Position = transform.TransformPoint(shoot.Position),
                    Rotation = quaternion.LookRotation(shoot.Forward, math.up()),
                    Scale = 1f
                });
                ECB.SetComponent(index, projectile, new PhysicsVelocity { Linear = shoot.Forward * shoot.Speed });
                // Reset cooldown
                shoot.CurrentCooldown = shoot.MaxCooldown;
                shoots[i] = shoot;
            }
        }
    }
}