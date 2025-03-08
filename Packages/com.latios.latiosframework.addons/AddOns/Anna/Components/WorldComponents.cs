using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Latios.Anna
{
    public struct PhysicsSettings : IComponentData
    {
        public CollisionLayerSettings collisionLayerSettings;
        public float3                 gravity;
        public half                   linearDamping;
        public half                   angularDamping;
        public byte                   numIterations;
    }

    public struct EnvironmentCollisionTag : IComponentData { }

    public partial struct EnvironmentCollisionLayer : ICollectionComponent
    {
        public CollisionLayer layer;

        public JobHandle TryDispose(JobHandle inputDeps) => layer.IsCreated ? layer.Dispose(inputDeps) : inputDeps;
    }

    public struct KinematicCollisionTag : IComponentData { }

    public partial struct KinematicCollisionLayer : ICollectionComponent
    {
        public CollisionLayer layer;

        public JobHandle TryDispose(JobHandle inputDeps) => layer.IsCreated ? layer.Dispose(inputDeps) : inputDeps;
    }
}

