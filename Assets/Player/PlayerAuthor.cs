using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public class PlayerAuthor : BaseAuthor
{
    public GameObject projectile;
    public List<GameObject> projectiles;
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new PlayerData
        {
            Health = 100,
            Shield = 100,
            AttackIndex = int.MaxValue,
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
    public int AttackIndex;
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
    
    public void OnCreate(ref SystemState state)
    {
        _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
    }

    public void OnDestroy(ref SystemState state) { }
    
    public void OnUpdate(ref SystemState state)
    {
        _localTransformLookup.Update(ref state);
        
        var deltaTime = SystemAPI.Time.DeltaTime;
        var bullets = PlayerManager.Bullets;
        EntityCommandBuffer ecb = GetEntityCommandBuffer(ref state);

        Entity player = SystemAPI.GetSingletonEntity<PlayerAspect>();
        PlayerAspect p = SystemAPI.GetAspect<PlayerAspect>(player);
        var projectiles = SystemAPI.GetBuffer<PlayerProjectilePrefab>(player);
        
        PlayerData pData = p.Player;

        if (PlayerManager.fire)
        {
            pData.AttackTime = 0;
            pData.AttackIndex = 0;
            PlayerManager.Fire();
        }
        pData.AttackTime += deltaTime;
        foreach (var attack in bullets)
        {
            var bulletQueue = attack.Bullets;
            
            var prefab = projectiles.ElementAt((int)attack.projectile).Projectile;
            
            while (bulletQueue.Count > 0 && pData.AttackTime > bulletQueue.Peek().time)
            {
                var bulletData = bulletQueue.Dequeue();
            
                Entity newEntity = ecb.Instantiate(prefab);
                var originalTransform = _localTransformLookup[prefab];
            
                var position = p.Transform.TransformPoint(bulletData.position);
                var rotation = math.mul(PlayerManager.main.LookRotation, bulletData.rotation);
            
                ecb.SetComponent(newEntity, LocalTransform.FromPositionRotationScale(
                    position,
                    rotation,
                    originalTransform.Scale
                ));
                ecb.SetComponent(newEntity, new Lifetime(){Time = attack.lifetime});
                var v = new PhysicsVelocity { Linear = math.mul(rotation, math.forward()) * attack.speed };
                ecb.AddComponent(newEntity, v);

                pData.AttackIndex++;
            }
        }
        
        p.Player = pData;
        
    }
    
    private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb;
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
            var impulse = playerData.GetMovement(player.PhysicsVelocity.Linear) * dt + playerData.GetDash();
            

            playerPhysics.ApplyImpulse(player.PhysicsMass, transform.Position, transform.Rotation, impulse, transform.Position);
            //playerPhysics.ApplyAngularImpulse(player.PhysicsMass, playerData.GetRotation(transform.Rotation) * dt);
            playerPhysics.Angular = float3.zero;
            playerData.position = player.Transform.Position;
            transform.Rotation = playerData.transform.rotation;
            
            player.Transform = transform;
            player.PhysicsVelocity = playerPhysics;
        }
        
    }
}