using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

public struct ProjectileData : IComponentData
{
    public float3 Velocity;
    public float Lifetime;
}

// Aspects must be declared as a readonly partial struct
public readonly partial struct ProjectileAspect : IAspect
{
    // An Entity field in an Aspect gives access to the Entity itself.
    // This is required for registering commands in an EntityCommandBuffer for example.
    public readonly Entity Self;

    // Aspects can contain other aspects.

    // A RefRW field provides read write access to a component. If the aspect is taken as an "in"
    // parameter, the field behaves as if it was a RefRO and throws exceptions on write attempts.
    private readonly RefRW<LocalTransform> _transform;
    private readonly RefRW<ProjectileData> _indData;
    private readonly RefRW<PhysicsVelocity> _velocity;

    public LocalTransform Transform
    {
        get => _transform.ValueRW;
        set => _transform.ValueRW = value;
    }
    
    public ProjectileData Data
    {
        get => _indData.ValueRW;
        set => _indData.ValueRW = value;
    }

    public PhysicsVelocity PhysicsVelocity
    {
        get => _velocity.ValueRW;
        set => _velocity.ValueRW = value;
    }
}