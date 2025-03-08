using Latios.Psyshock;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Latios.Anna
{
    internal partial struct CapturedRigidBodies : ICollectionComponent
    {
        public NativeArray<CapturedRigidBodyState> states;  // src indices
        public NativeParallelHashMap<Entity, int>  entityToSrcIndexMap;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // WorldUpdateAllocator
    }

    internal partial struct CapturedKinematics : ICollectionComponent
    {
        public NativeArray<CapturedKinematic>     kinematics;  // src indices
        public NativeParallelHashMap<Entity, int> entityToSrcIndexMap;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // WorldUpdateAllocator
    }

    internal partial struct BodyVsEnvironmentPairStream : ICollectionComponent
    {
        public PairStream pairStream;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // WorldUpdateAllocator
    }

    internal partial struct BodyVsKinematicPairStream : ICollectionComponent
    {
        public PairStream pairStream;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // WorldUpdateAllocator
    }

    internal partial struct BodyVsBodyPairStream : ICollectionComponent
    {
        public PairStream pairStream;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // WorldUpdateAllocator
    }

    // LockWorldAxesFlags
    internal partial struct BodyConstraintsPairStream : ICollectionComponent
    {
        public PairStream pairStream;

        public JobHandle TryDispose(JobHandle inputDeps) => inputDeps;  // WorldUpdateAllocator
    }
}

