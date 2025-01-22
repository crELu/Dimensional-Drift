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
    public override void Bake(UniversalBaker baker, Entity entity)
    {
        baker.AddComponent(entity, new Player());
        base.Bake(baker, entity);
    }
}

public struct Player : IComponentData
{
}

public readonly partial struct PlayerAspect : IAspect
{
    public readonly Entity Self;
    
    private readonly RefRW<Player> _player;
    private readonly RefRW<LocalTransform> _localTransform;
    private readonly RefRW<PhysicsVelocity> _physicsVelocity;
    private readonly RefRO<PhysicsMass> _physicsMass;

    public Player Player {
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
            var f = playerData.GetMovement(player.PhysicsVelocity.Linear) * dt + playerData.GetDash();
        
            playerPhysics.ApplyImpulse(player.PhysicsMass, transform.Position, transform.Rotation, f, transform.Position);
            //playerPhysics.ApplyAngularImpulse(player.PhysicsMass, playerData.GetRotation(transform.Rotation) * dt);
        
            playerData.position = player.Transform.Position;
            transform.Rotation = math.slerp(transform.Rotation, playerData.transform.rotation, .5f);
            player.Transform = transform;
            player.PhysicsVelocity = playerPhysics;
        }
        
    }
}