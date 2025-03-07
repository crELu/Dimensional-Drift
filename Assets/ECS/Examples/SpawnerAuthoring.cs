using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[StructLayout(LayoutKind.Sequential)][System.Serializable]
public struct SpawnerProjData {  
    public float Speed, Lifetime;
}

[StructLayout(LayoutKind.Sequential)][System.Serializable]
public struct SpawnerData {
    public float SpawnRate, RotationSpeed, Count;
    public float2 Arc;
}

public struct Spawner : IComponentData
{
    public Entity Prefab;
    public float NextSpawnTime;
    public SpawnerData Data;
    public SpawnerProjData ProjData;
}

public readonly partial struct SpawnerAspect : IAspect
{
    public readonly Entity Self;
    
    readonly RefRW<LocalTransform> localTransform;
    readonly RefRW<Spawner> spawner;
    public LocalTransform Transform {
        get => localTransform.ValueRW;
        set => localTransform.ValueRW = value;
    }
    public Entity Prefab => spawner.ValueRO.Prefab;
    public float NextSpawnTime
    {
        get => spawner.ValueRO.NextSpawnTime;
        set => spawner.ValueRW.NextSpawnTime = value;
    }

    public SpawnerProjData ProjData => spawner.ValueRO.ProjData;
    public SpawnerData Data => spawner.ValueRO.Data;
}

public class SpawnerAuthoring : MonoBehaviour
{
    public GameObject prefab;
    public SpawnerProjData projData;
    public SpawnerData data;
}

class SpawnerBaker : Baker<SpawnerAuthoring>
{
    public override void Bake(SpawnerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent(entity, new Spawner
        {
            Prefab = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic),
            NextSpawnTime = 0.0f,
            Data = authoring.data,
            ProjData = authoring.projData
        });
    }
}