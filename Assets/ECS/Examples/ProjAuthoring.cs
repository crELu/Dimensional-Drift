using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

class ProjAuthoring : MonoBehaviour
{
    public GameObject Prefab;
    public float SpawnRate;
}

class ProjBaker : Baker<ProjAuthoring>
{
    public override void Bake(ProjAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        //AddComponent(entity, new BillboardData{ExtraRot = quaternion.identity});
        // AddComponent(entity, new Spawner
        // {
        //     // By default, each authoring GameObject turns into an Entity.
        //     // Given a GameObject (or authoring component), GetEntity looks up the resulting Entity.
        //     Prefab = GetEntity(authoring.Prefab, TransformUsageFlags.Dynamic),
        //     SpawnPosition = authoring.transform.position,
        //     NextSpawnTime = 0.0f,
        //     SpawnRate = authoring.SpawnRate
        // });
    }
}