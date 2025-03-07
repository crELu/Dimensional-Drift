using Latios.Psyshock;
using Latios.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

using static Unity.Entities.SystemAPI;

namespace Latios.Anna
{
    [DisableAutoCreation]
    [BurstCompile]
    public partial struct RigidBodyVsKinematicSystem : ISystem, ISystemNewScene
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        public void OnNewScene(ref SystemState state)
        {
            latiosWorld.sceneBlackboardEntity.AddOrSetCollectionComponentAndDisposeOld<BodyVsKinematicPairStream>(default);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var rigidBodyLayer             = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<RigidBodyCollisionLayer>(true).layer;
            var kinematicLayer             = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<KinematicCollisionLayer>(true).layer;
            var states                     = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<CapturedRigidBodies>(true).states;
            var kinematics                 = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<CapturedKinematics>(true).kinematics;
            var pairStream                 = new PairStream(rigidBodyLayer, state.WorldUpdateAllocator);
            var findBodyKinematicProcessor = new FindBodyVsKinematicProcessor
            {
                states           = states,
                kinematics       = kinematics,
                pairStream       = pairStream.AsParallelWriter(),
                deltaTime        = Time.DeltaTime,
                inverseDeltaTime = math.rcp(Time.DeltaTime),
            };
            state.Dependency = Physics.FindPairs(in rigidBodyLayer, in kinematicLayer, in findBodyKinematicProcessor)
                               .ScheduleParallelUnsafe(state.Dependency);
            latiosWorld.sceneBlackboardEntity.SetCollectionComponentAndDisposeOld(new BodyVsKinematicPairStream { pairStream = pairStream });
        }

        struct FindBodyVsKinematicProcessor : IFindPairsProcessor
        {
            [ReadOnly] public NativeArray<CapturedRigidBodyState> states;
            [ReadOnly] public NativeArray<CapturedKinematic>      kinematics;
            public PairStream.ParallelWriter                      pairStream;
            public float                                          deltaTime;
            public float                                          inverseDeltaTime;

            DistanceBetweenAllCache distanceBetweenAllCache;

            public void Execute(in FindPairsResult result)
            {
                ref readonly var rigidBodyA        = ref states.AsReadOnlySpan()[result.sourceIndexA];
                var              kinematicVelocity = kinematics[result.sourceIndexB].velocity;

                var maxDistance = UnitySim.MotionExpansion.GetMaxDistance(in rigidBodyA.motionExpansion);
                Physics.DistanceBetweenAll(result.colliderA, result.transformA, result.colliderB, result.transformB, maxDistance, ref distanceBetweenAllCache);
                foreach (var distanceResult in distanceBetweenAllCache)
                {
                    var contacts = UnitySim.ContactsBetween(result.colliderA, result.transformA, result.colliderB, result.transformB, in distanceResult);

                    ref var streamData           = ref pairStream.AddPairAndGetRef<ContactStreamData>(result.pairStreamKey, true, false, out var pair);
                    streamData.contactParameters = pair.Allocate<UnitySim.ContactJacobianContactParameters>(contacts.contactCount, NativeArrayOptions.UninitializedMemory);
                    streamData.contactImpulses   = pair.Allocate<float>(contacts.contactCount, NativeArrayOptions.ClearMemory);
                    streamData.indexA            = result.sourceIndexA;
                    streamData.indexB            = result.sourceIndexB;
                    pair.userByte                = SolveByteCodes.contactKinematic;

                    UnitySim.BuildJacobian(streamData.contactParameters.AsSpan(),
                                           out streamData.bodyParameters,
                                           rigidBodyA.inertialPoseWorldTransform,
                                           in rigidBodyA.velocity,
                                           in rigidBodyA.mass,
                                           RigidTransform.identity,
                                           in kinematicVelocity,
                                           default,
                                           contacts.contactNormal,
                                           contacts.AsSpan(),
                                           rigidBodyA.coefficientOfRestitution,
                                           rigidBodyA.coefficientOfFriction,
                                           UnitySim.kMaxDepenetrationVelocityDynamicStatic,
                                           math.max(0f, math.dot(rigidBodyA.gravity, -contacts.contactNormal)),
                                           deltaTime,
                                           inverseDeltaTime);
                }
            }
        }
    }
}

