
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
//
// public enum BulletAddons
// {
//     Homing,
//     RocketSwarm,
//     Fragmentation,
//     ClusterMunitions,
//     Acceleration,
// }
//
// public struct MultiShootComponent : IComponentData
// {
//     public int Count;          // Number of projectiles
//     public float SpreadAngle;  // Spread angle (e.g., 30 degrees)
//     public float Speed;        // Speed for projectiles
// }
//
// public struct ProjectileComponentBuffer : IBufferElementData
// {
//     public Entity ComponentEntity;
// }
//
// [BurstCompile]
// public partial struct MultiProjectileSpawnJob : IJobEntity
// {
//     public EntityCommandBuffer.ParallelWriter ECB;
//     [ReadOnly] public BufferLookup<ProjectileComponentBuffer> BufferLookup;
//     public Entity ProjectilePrefab;
//     [ReadOnly] public NativeArray<IComponentData> s;
//
//     public void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, in LocalToWorld transform, in MultiShootComponent shootData)
//     {
//         int count = shootData.Count;
//         float spreadAngle = shootData.SpreadAngle;
//         float baseSpeed = shootData.Speed;
//         float3 forward = transform.Forward;
//
//         for (int i = 0; i < count; i++)
//         {
//             // Compute the angle offset based on spread
//             float angleOffset = (spreadAngle / (count - 1)) * (i - (count - 1) / 2.0f); 
//             quaternion rotation = quaternion.AxisAngle(math.up(), math.radians(angleOffset));
//             float3 direction = math.mul(rotation, forward);
//
//             // Spawn projectile
//             Entity projectile = ECB.Instantiate(chunkIndex, ProjectilePrefab);
//             
//
//             // Add arbitrary components (if any)
//             if (BufferLookup.HasBuffer(entity))
//             {
//                 DynamicBuffer<ProjectileComponentBuffer> buffer = BufferLookup[entity];
//                 foreach (var bufferElement in buffer)
//                 {
//                     var componentData = bufferElement.ComponentEntity;
//                     //TODO
//                     
//                     ProjectileComponentRegistry.AddComponent(ECB, chunkIndex, projectile, component);
//                 }
//             }
//         }
//
//         // Remove flag after processing
//         ECB.RemoveComponent<MultiShootComponent>(chunkIndex, entity);
//     }
// }
