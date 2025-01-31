using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Entities.Graphics;
using Unity.Jobs;
using Unity.Rendering;
using UnityEngine;

// [BurstCompile]
public partial struct SquishSystem : ISystem
{
    private static readonly int T = Shader.PropertyToID("_SquishData");

    public void OnCreate(ref SystemState state) { }

    public void OnDestroy(ref SystemState state) { }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var renderMeshArrayList = new List<RenderMeshArray>();

        // Get all unique RenderMeshArray components
        state.EntityManager.GetAllUniqueSharedComponentsManaged<RenderMeshArray>(renderMeshArrayList);

        foreach (var renderMeshArray in renderMeshArrayList)
        {
            if (renderMeshArray.MaterialReferences != null)
            {
                foreach (var material in renderMeshArray.MaterialReferences)
                {
                    material.Value.SetVector(T, SquishManager.data);
                }
            }
        }

        // Dispose the NativeList to avoid memory leaks
        renderMeshArrayList.Clear();
        
    }

    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}

// // [BurstCompile]
// public partial struct ProcessSquishJob : IJobEntity
// {
//     public float T1, H, T2;
//     public float2 D;
//     private static readonly int T = Shader.PropertyToID("_t");
//     private static readonly int T3 = Shader.PropertyToID("_t2");
//     private static readonly int Line = Shader.PropertyToID("_line");
//     private static readonly int H1 = Shader.PropertyToID("_h");
//
//     // IJobEntity generates a component data query based on the parameters of its `Execute` method.
//     // This example queries for all Spawner components and uses `ref` to specify that the operation
//     // requires read and write access. Unity processes `Execute` for each entity that matches the
//     // component data query. 
//     private void Execute([ChunkIndexInQuery] int chunkIndex, RenderMeshArray p)
//     {
//         var material = p.MaterialReferences[0];
//         material.Value.SetFloat(T, T1);
//         material.Value.SetFloat(T3, T2);
//         material.Value.SetFloat(H1, H);
//         material.Value.SetVector(Line, (Vector2)D); 
//         Debug.Log("uhohj");
//     }
// }