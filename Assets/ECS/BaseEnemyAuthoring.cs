using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

class BaseEnemyAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnRate;
}

class BaseEnemyBaker : Baker<BaseEnemyAuthoring>
{
    public override void Bake(BaseEnemyAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new EnemyHealth
        {
            Health = 10,
            MaxHealth = 10
        });
        AddComponent(entity, new PhysicsVelocity
        {
        });
    }
}