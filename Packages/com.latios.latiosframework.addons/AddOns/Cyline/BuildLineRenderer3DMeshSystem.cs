using System;
using Latios.Kinemation;
using Latios.Kinemation.Systems;
using static Unity.Entities.SystemAPI;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;

namespace Latios.Cyline.Systems
{
    [UpdateInGroup(typeof(UpdatePresentationSystemGroup))]
    [UpdateBefore(typeof(KinemationRenderUpdateSuperSystem))]
    [RequireMatchingQueriesForUpdate]
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct BuildLineRenderer3DMeshSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;
        EntityQuery          m_query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
            var fluent  = state.Fluent().With<UniqueMeshConfig, RenderBounds>(false).With<UniqueMeshPosition, UniqueMeshNormal, UniqueMeshIndex>(false)
                          .With<LineRenderer3DPoint>(true);
            if ((state.WorldUnmanaged.Flags & WorldFlags.Editor) == WorldFlags.Editor)
                fluent = fluent.With<LineRenderer3DConfig>(false);
            else
                fluent = fluent.WithEnabled<LineRenderer3DConfig>(false);
            m_query    = fluent.Build();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var jh           = new Job().ScheduleParallel(m_query, state.Dependency);
            state.Dependency = new CleanupJob
            {
                handle = GetComponentTypeHandle<LineRenderer3DConfig>(false)
            }.Schedule(m_query, jh);
        }

        // The code within the following job struct is a derivative of the algorithm found here: https://github.com/survivorr9049/LineRenderer3D
        // The original work is licensed as follows:
        //
        // MIT License
        //
        // Copyright(c) 2024 survivorr
        //
        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files(the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:
        //
        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.
        //
        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.

        [BurstCompile]
        partial struct Job : IJobEntity
        {
            UnsafeList<Node> nodes;

            public void Execute(EnabledRefRW<UniqueMeshConfig>        enableMeshRebuild,
                                ref DynamicBuffer<UniqueMeshPosition> positionBuffer,
                                ref DynamicBuffer<UniqueMeshNormal>   normalBuffer,
                                ref DynamicBuffer<UniqueMeshIndex>    indexBuffer,
                                in DynamicBuffer<LineRenderer3DPoint> pointsBuffer,
                                in LineRenderer3DConfig config,
                                ref RenderBounds bounds)
            {
                enableMeshRebuild.ValueRW = true;
                positionBuffer.Clear();
                normalBuffer.Clear();
                indexBuffer.Clear();
                if (config.resolution == 0)
                    return;
                if (pointsBuffer.Length < 2)
                    return;

                var points = pointsBuffer.AsNativeArray();

                // Initialize nodes
                if (!nodes.IsCreated)
                    nodes = new UnsafeList<Node>(points.Length, Allocator.Temp);
                nodes.Clear();
                nodes.Resize(points.Length);
                for (int i = 0; i < points.Length; i++)
                    nodes[i] = new Node { position = points[i].position, thickness = points[i].thickness };

                CalculatePointData();
                CalculateEdgePoints();
                FixPointsRotation();

                Span<float> cosines = stackalloc float[config.resolution];
                Span<float> sines   = stackalloc float[config.resolution];
                for (int i = 0; i < config.resolution; i++)
                {
                    math.sincos(i * math.TAU / config.resolution, out var s, out var c);
                    cosines[i] = c;
                    sines[i]   = s;
                }

                // The Line3D algorithm
                int resolution = config.resolution;
                int iterations = points.Length;
                positionBuffer.ResizeUninitialized(iterations * resolution);
                normalBuffer.ResizeUninitialized(positionBuffer.Length);
                indexBuffer.ResizeUninitialized((iterations - 1) * resolution * 6);
                var positions = positionBuffer.AsNativeArray().Reinterpret<float3>();
                var normals   = normalBuffer.AsNativeArray().Reinterpret<float3>();
                var indices   = indexBuffer.AsNativeArray().Reinterpret<int>();

                float3 min = nodes[0].position;
                float3 max = min;
                for (int i = 0; i < iterations; i++)
                {
                    var nodeI = nodes[i];
                    var right = nodeI.right * nodeI.thickness;
                    var up    = nodeI.up * nodeI.thickness;
                    for (int j = 0; j < resolution; j++)
                    {
                        var index         = i * resolution + j;
                        positions[index]  = nodeI.position;
                        var vertexOffset  = cosines[j] * right + sines[j] * up;
                        normals[index]    = math.normalizesafe(vertexOffset);
                        var nn            = math.normalizesafe(nodeI.normal);
                        vertexOffset     += nn * math.dot(nn, vertexOffset) * (math.clamp(1f / math.length(nodeI.normal), 0f, 2f) - 1f);
                        positions[index] += vertexOffset;
                        min               = math.min(min, positions[index]);
                        max               = math.max(max, positions[index]);
                        if (i == iterations - 1)
                            continue;
                        int offset          = i * resolution * 6 + j * 6;
                        indices[offset]     = j + i * resolution;
                        indices[offset + 1] = (j + 1) % resolution + i * resolution;
                        indices[offset + 2] = j + resolution + i * resolution;
                        indices[offset + 3] = (j + 1) % resolution + i * resolution;
                        indices[offset + 4] = (j + 1) % resolution + resolution + i * resolution;
                        indices[offset + 5] = j + resolution + i * resolution;
                    }
                }
                bounds.SetMinMax(min, max);
            }

            struct Node
            {
                public float3 position;
                public float3 direction;
                public float3 normal;
                public float3 up;
                public float3 right;
                public float  thickness;
            }

            void CalculatePointData()
            {
                for (int i = 1; i + 1 < nodes.Length; i++)
                {
                    ref var n         = ref nodes.ElementAt(i);
                    var     previous  = math.normalizesafe(n.position - nodes[i - 1].position);
                    var     next      = math.normalizesafe(nodes[i + 1].position - n.position);
                    var     direction = math.normalizesafe((previous + next) * 0.5f);
                    var     normal    = math.normalizesafe(next - previous) * math.abs(math.dot(previous, direction));  // length encodes cosine of angle
                    var     right     = math.normalizesafe(math.cross(direction, math.right()));
                    if (right.Equals(float3.zero))
                        right   = math.normalizesafe(math.cross(direction, math.forward()));
                    var up      = math.normalizesafe(math.cross(direction, right));
                    n.direction = direction;
                    n.normal    = normal;
                    n.up        = up;
                    n.right     = right;
                }
            }

            void CalculateEdgePoints()
            {
                var     edgeDirection = math.normalizesafe(nodes[1].position - nodes[0].position);
                var     edgeRight     = math.normalizesafe(math.cross(edgeDirection, math.right()));
                var     edgeUp        = math.normalizesafe(math.cross(edgeDirection, edgeRight));
                ref var firstNode     = ref nodes.ElementAt(0);
                firstNode.direction   = edgeDirection;
                firstNode.normal      = float3.zero;
                firstNode.right       = edgeRight;
                firstNode.up          = edgeUp;
                edgeDirection         = math.normalizesafe(nodes[nodes.Length - 1].position, nodes[nodes.Length - 2].position);
                edgeRight             = math.normalizesafe(math.cross(edgeDirection, math.right()));
                edgeUp                = math.normalizesafe(math.cross(edgeDirection, edgeRight));
                ref var lastNode      = ref nodes.ElementAt(nodes.Length - 1);
                lastNode.direction    = edgeDirection;
                lastNode.normal       = float3.zero;
                lastNode.right        = edgeRight;
                lastNode.up           = edgeUp;
            }

            void FixPointsRotation()
            {
                for (int i = 0; i + 1 < nodes.Length; i++)
                {
                    ref var n1          = ref nodes.ElementAt(i + 1);
                    var     n0          = nodes[i];
                    var     fromTo      = math.normalizesafe(n1.position - n0.position);
                    var     firstRight  = n0.right - math.dot(n0.right, fromTo) * fromTo;
                    var     secondRight = n1.right - math.dot(n1.right, fromTo) * fromTo;
                    var     angle       = -UnityEngine.Vector3.SignedAngle(firstRight, secondRight, fromTo);
                    var     rot         = UnityEngine.Quaternion.AngleAxis(angle, n1.direction);
                    n1.up               = math.rotate(rot, n1.up);
                    n1.right            = math.rotate(rot, n1.right);
                }
            }
        }

        [BurstCompile]
        partial struct CleanupJob : IJobChunk
        {
            public ComponentTypeHandle<LineRenderer3DConfig> handle;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                chunk.SetComponentEnabledForAll(ref handle, false);
            }
        }
    }
}

