using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Latios.Cyline
{
    /// <summary>
    /// Defines the settings for the Line Renderer 3D. When enabled, the mesh will be regenerated.
    /// </summary>
    public struct LineRenderer3DConfig : IComponentData, IEnableableComponent
    {
        public short resolution;
    }

    /// <summary>
    /// Defines a point for the Line Renderer 3D. Populate it to draw a line.
    /// </summary>
    [Serializable]
    [InternalBufferCapacity(0)]
    public struct LineRenderer3DPoint : IBufferElementData
    {
        public float3 position;
        public float  thickness;
    }
}

