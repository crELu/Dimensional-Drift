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
    public partial struct SolveSystem : ISystem
    {
        LatiosWorldUnmanaged latiosWorld;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            latiosWorld = state.GetLatiosWorldUnmanaged();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var physicsSettings = latiosWorld.GetPhysicsSettings();
            var pairStream      = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<BodyVsEnvironmentPairStream>(false).pairStream;
            var b               = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<BodyVsKinematicPairStream>(false).pairStream;
            var c               = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<BodyVsBodyPairStream>(false).pairStream;
            var d               = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<BodyConstraintsPairStream>(false).pairStream;

            var states     = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<CapturedRigidBodies>(false).states;
            var kinematics = latiosWorld.sceneBlackboardEntity.GetCollectionComponent<CapturedKinematics>(true).kinematics;

            var jh = new CombineStreamsJob { a = pairStream, b = b, c = c, d = d }.Schedule(state.Dependency);

            int numIterations  = physicsSettings.numIterations;
            var solveProcessor = new SolveBodiesProcessor
            {
                states                 = states,
                kinematics             = kinematics,
                invNumSolverIterations = math.rcp(numIterations),
                deltaTime              = Time.DeltaTime,
                inverseDeltaTime       = math.rcp(Time.DeltaTime),
                firstIteration         = true,
                lastIteration          = false,
                icb                    = latiosWorld.syncPoint.CreateInstantiateCommandBuffer<WorldTransform>().AsParallelWriter(),
            };
            var stabilizerJob = new StabilizeRigidBodiesJob
            {
                states         = states,
                firstIteration = true,
                dt             = Time.DeltaTime
            };
            for (int i = 0; i < numIterations; i++)
            {
                jh                            = Physics.ForEachPair(in pairStream, in solveProcessor).ScheduleParallel(jh);
                jh                            = stabilizerJob.ScheduleParallel(states.Length, 128, jh);
                solveProcessor.firstIteration = false;
                solveProcessor.lastIteration  = i + 2 == numIterations;
                stabilizerJob.firstIteration  = false;
            }
            state.Dependency = jh;
        }

        [BurstCompile]
        struct CombineStreamsJob : IJob
        {
            public PairStream a;
            public PairStream b;
            public PairStream c;
            public PairStream d;

            public void Execute()
            {
                a.ConcatenateFrom(ref b);
                a.ConcatenateFrom(ref c);
                a.ConcatenateFrom(ref d);
            }
        }

        [BurstCompile]
        struct StabilizeRigidBodiesJob : IJobFor
        {
            [NativeDisableParallelForRestriction] public NativeArray<CapturedRigidBodyState> states;

            public float dt;
            public bool  firstIteration;

            public void Execute(int index)
            {
                ref var rigidBody = ref states.AsSpan()[index];
                UnitySim.UpdateStabilizationAfterSolverIteration(ref rigidBody.motionStabilizer,
                                                                 ref rigidBody.velocity,
                                                                 rigidBody.mass.inverseMass,
                                                                 rigidBody.angularExpansion,
                                                                 rigidBody.numOtherSignificantBodiesInContact,
                                                                 dt * rigidBody.gravity,
                                                                 math.normalize(rigidBody.gravity),
                                                                 UnitySim.kDefaultVelocityClippingFactor,
                                                                 UnitySim.kDefaultInertialScalingFactor,
                                                                 firstIteration);
            }
        }
    }
}

