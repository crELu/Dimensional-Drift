using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Latios.Anna.Systems
{
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct BuildKinematicCollisionLayerSystem : ISystem, ISystemNewScene
    {
        LatiosWorldUnmanaged latiosWorld;
        EntityQuery          m_query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();

            m_query = state.Fluent().With<KinematicCollisionTag, PreviousTransform>(true).PatchQueryForBuildingCollisionLayer().Build();
        }

        public void OnNewScene(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<KinematicCollisionLayer>(default);
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<CapturedKinematics>(     default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsSettings = latiosWorld.GetPhysicsSettings();
            var count           = m_query.CalculateEntityCountWithoutFiltering();
            if (count == 0)
            {
                latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new KinematicCollisionLayer
                {
                    layer = CollisionLayer.CreateEmptyCollisionLayer(physicsSettings.collisionLayerSettings, state.WorldUpdateAllocator)
                });
                latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new CapturedKinematics
                {
                    entityToSrcIndexMap = new NativeParallelHashMap<Entity, int>(1, state.WorldUpdateAllocator),
                    kinematics          = CollectionHelper.CreateNativeArray<CapturedKinematic>(0, state.WorldUpdateAllocator)
                });
                return;
            }

            var startIndices     = m_query.CalculateBaseEntityIndexArrayAsync(state.WorldUpdateAllocator, default, out var jh);
            var colliderBodies   = CollectionHelper.CreateNativeArray<ColliderBody>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var aabbs            = CollectionHelper.CreateNativeArray<Aabb>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var kinematics       = CollectionHelper.CreateNativeArray<CapturedKinematic>(count, state.WorldUpdateAllocator, NativeArrayOptions.UninitializedMemory);
            var entityToIndexMap = new NativeParallelHashMap<Entity, int>(count, state.WorldUpdateAllocator);
            jh                   = new Job
            {
                aabbs                   = aabbs,
                bucketCalculator        = new CollisionLayerBucketIndexCalculator(in physicsSettings.collisionLayerSettings),
                colliderBodies          = colliderBodies,
                colliderHandle          = GetComponentTypeHandle<Collider>(true),
                dt                      = Time.DeltaTime,
                entityHandle            = GetEntityTypeHandle(),
                entityToIndexMap        = entityToIndexMap.AsParallelWriter(),
                physicsSettings         = physicsSettings,
                previousTransformHandle = GetComponentTypeHandle<PreviousTransform>(true),
                startIndices            = startIndices,
                kinematics              = kinematics,
                transformHandle         = GetComponentTypeHandle<WorldTransform>(true)
            }.ScheduleParallel(m_query, JobHandle.CombineDependencies(state.Dependency, jh));

            jh = Physics.BuildCollisionLayer(colliderBodies, aabbs).WithSettings(physicsSettings.collisionLayerSettings)
                 .ScheduleParallel(out var layer, state.WorldUpdateAllocator, jh);

            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new KinematicCollisionLayer
            {
                layer = layer
            });
            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new CapturedKinematics
            {
                entityToSrcIndexMap = entityToIndexMap,
                kinematics          = kinematics
            });
            state.Dependency = jh;
        }

        [BurstCompile]
        partial struct Job : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle                       entityHandle;
            [ReadOnly] public ComponentTypeHandle<WorldTransform>    transformHandle;
            [ReadOnly] public ComponentTypeHandle<PreviousTransform> previousTransformHandle;
            [ReadOnly] public ComponentTypeHandle<Collider>          colliderHandle;

            [ReadOnly] public NativeArray<int> startIndices;

            [NativeDisableParallelForRestriction] public NativeArray<CapturedKinematic> kinematics;
            [NativeDisableParallelForRestriction] public NativeArray<ColliderBody>      colliderBodies;
            [NativeDisableParallelForRestriction] public NativeArray<Aabb>              aabbs;
            public NativeParallelHashMap<Entity, int>.ParallelWriter                    entityToIndexMap;

            public PhysicsSettings                     physicsSettings;
            public CollisionLayerBucketIndexCalculator bucketCalculator;
            public float                               dt;

            public unsafe void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entities           = chunk.GetEntityDataPtrRO(entityHandle);
                var transforms         = (WorldTransform*)chunk.GetRequiredComponentDataPtrRO(ref transformHandle);
                var colliders          = (Collider*)chunk.GetRequiredComponentDataPtrRO(ref colliderHandle);
                var previousTransforms = (PreviousTransform*)chunk.GetRequiredComponentDataPtrRO(ref previousTransformHandle);

                for (int i = 0, index = startIndices[unfilteredChunkIndex]; i < chunk.Count; i++, index++)
                {
                    entityToIndexMap.TryAdd(entities[i], index);

                    ref var transform = ref transforms[i];
                    ref var previous  = ref previousTransforms[i];
                    ref var collider  = ref colliders[i];

                    colliderBodies[index] = new ColliderBody
                    {
                        collider  = collider,
                        transform = transform.worldTransform,
                        entity    = entities[i],
                    };

                    var aabb              = Physics.AabbFrom(in collider, in transform.worldTransform);
                    var angularExpansion  = UnitySim.AngularExpansionFactorFrom(in collider);
                    var localCenterOfMass = UnitySim.LocalCenterOfMassFrom(in collider);
                    var localInertia      = UnitySim.LocalInertiaTensorFrom(in collider, transform.stretch);
                    UnitySim.ConvertToWorldMassInertia(in transform.worldTransform,
                                                       in localInertia,
                                                       localCenterOfMass,
                                                       0f,
                                                       out var mass,
                                                       out var inertialPoseWorldTransform);
                    var rotationDelta      = math.mul(transform.rotation, math.inverse(previous.rotation));
                    var rotationDeltaLocal = math.InverseRotateFast(inertialPoseWorldTransform.rot, rotationDelta);
                    var velocity           = new UnitySim.Velocity
                    {
                        linear  = transform.position - previous.position,
                        angular = math.Euler(rotationDeltaLocal) / dt
                    };

                    var motionExpansion = new UnitySim.MotionExpansion(in velocity, dt, angularExpansion);
                    aabb                = motionExpansion.ExpandAabb(aabb);

                    aabbs[index] = aabb;

                    kinematics[index] = new CapturedKinematic { velocity = velocity, inertialPoseWorldTransform = inertialPoseWorldTransform };
                }
            }
        }
    }
}

