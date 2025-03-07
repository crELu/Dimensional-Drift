using System.Collections.Generic;
using Latios.Authoring;
using Latios.Kinemation;
using Latios.Kinemation.Authoring;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Latios.Cyline.Authoring
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Latios/Cyline/Line Renderer 3D (Cyline)")]
    public class LineRenderer3DAuthoring : MonoBehaviour, IOverrideMeshRenderer
    {
        public List<LineRenderer3DPoint> points;
        public short                     resolution = 8;
    }

    public class LineRenderer3DAuthoringBaker : Baker<LineRenderer3DAuthoring>
    {
        public override void Bake(LineRenderer3DAuthoring authoring)
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr == null)
                return;

            float3 min = float.MaxValue;
            float3 max = float.MinValue;

            if (authoring.points != null)
            {
                foreach (var p in authoring.points)
                {
                    min = math.min(min, p.position - p.thickness);
                    max = math.max(max, p.position + p.thickness);
                }
            }
            else
            {
                min = 0f;
                max = 0f;
            }

            var entity   = GetEntity(TransformUsageFlags.Renderable);
            var settings = new MeshRendererBakeSettings
            {
                renderMeshDescription       = new Unity.Rendering.RenderMeshDescription(mr),
                isDeforming                 = false,
                isStatic                    = false,
                lightmapIndex               = mr.lightmapIndex,
                lightmapScaleOffset         = mr.lightmapScaleOffset,
                useLightmapsIfPossible      = false,
                suppressDeformationWarnings = false,
                localBounds                 = new Bounds((min + max) * 0.5f, max - min),
                targetEntity                = entity,
            };
            this.BakeMeshAndMaterial(settings, RenderingBakingTools.uniqueMeshPlaceholder, mr.sharedMaterial);

            AddComponent(entity, new LineRenderer3DConfig { resolution = authoring.resolution });
            SetComponentEnabled<LineRenderer3DConfig>(entity, true);
            var pointBuffer = AddBuffer<LineRenderer3DPoint>(entity);
            if (authoring.points != null)
            {
                foreach (var p in authoring.points)
                    pointBuffer.Add(p);
            }

            AddComponent(entity, new UniqueMeshConfig { });
            AddBuffer<UniqueMeshPosition>(entity);
            AddBuffer<UniqueMeshNormal>(  entity);
            AddBuffer<UniqueMeshIndex>(   entity);
        }
    }
}

