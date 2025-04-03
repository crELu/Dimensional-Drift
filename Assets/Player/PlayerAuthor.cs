using System.Collections.Generic;
using Latios.Psyshock;
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
using Collider = Latios.Psyshock.Collider;
using Random = UnityEngine.Random;
using SphereCollider = Latios.Psyshock.SphereCollider;

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
            
            PlayerManager.burstPos.Data = transform.Position;
            player.PhysicsVelocity = playerPhysics;
            playerData.velocity.text = $"{math.length(player.PhysicsVelocity.Linear):F0}";
            RefRW<PhysicsSystemState> physicsState = SystemAPI.GetSingletonRW<PhysicsSystemState>();
            physicsState.ValueRO.GetInRadius(player.Transform.Position, 30f, physicsState.ValueRO.IntelLayer, out BodiesInRadius inRadius);
            foreach ((FindObjectsResult, PointDistanceResult) result in inRadius) {
                if (!state.EntityManager.Exists(result.Item1.entity)) continue;
                var intelPos = SystemAPI.GetComponent<LocalTransform>(result.Item1.entity);
                
                var intelVel = SystemAPI.GetComponent<PhysicsVelocity>(result.Item1.entity);
                var d = transform.Position - intelPos.Position;
                if (DimensionManager.CurrentDim == Dimension.Two) d.y = 0;
                intelVel.Linear = math.normalize(d) * 40;
                SystemAPI.SetComponent(result.Item1.entity, intelVel);
                //PhysicsDebug.DrawCollider(result.Item1.collider, result.Item1.transform, UnityEngine.Color.red);
            }
        }
    }
}