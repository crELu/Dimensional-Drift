using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Latios.Anna
{
    public struct RigidBody : IComponentData
    {
        public UnitySim.Velocity velocity;
        public float             inverseMass;
        public half              coefficientOfFriction;
        public half              coefficientOfRestitution;
    }

    [InternalBufferCapacity(0)]
    public struct AddImpulse : IBufferElementData
    {
        internal float3 pointOrField;
        internal float3 pointImpulseOrZero;

        public AddImpulse(float3 fieldImpulse)
        {
            pointOrField       = fieldImpulse;
            pointImpulseOrZero = float3.zero;
        }

        public AddImpulse(float3 worldPoint, float3 impulse)
        {
            pointOrField       = worldPoint;
            pointImpulseOrZero = impulse;
        }
    }

    public struct LockWorldAxesFlags : IComponentData
    {
        public byte packedFlags;

        public bool positionX
        {
            get => (packedFlags & 0x1) != 0;
            set => packedFlags = (byte)((packedFlags & ~0x1) | math.select(0, 0x1, value));
        }
        public bool positionY
        {
            get => (packedFlags & 0x2) != 0;
            set => packedFlags = (byte)((packedFlags & ~0x2) | math.select(0, 0x2, value));
        }
        public bool positionZ
        {
            get => (packedFlags & 0x4) != 0;
            set => packedFlags = (byte)((packedFlags & ~0x4) | math.select(0, 0x4, value));
        }
        public bool rotationX
        {
            get => (packedFlags & 0x8) != 0;
            set => packedFlags = (byte)((packedFlags & ~0x8) | math.select(0, 0x8, value));
        }
        public bool rotationY
        {
            get => (packedFlags & 0x10) != 0;
            set => packedFlags = (byte)((packedFlags & ~0x10) | math.select(0, 0x10, value));
        }
        public bool rotationZ
        {
            get => (packedFlags & 0x20) != 0;
            set => packedFlags = (byte)((packedFlags & ~0x20) | math.select(0, 0x20, value));
        }
    }

    public partial struct RigidBodyCollisionLayer : ICollectionComponent
    {
        public CollisionLayer layer;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // Uses WorldUpdateAllocator
    }
}

