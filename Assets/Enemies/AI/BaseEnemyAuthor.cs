using System;
using Latios;
using Latios.Authoring;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Serialization;
using LocalTransform = Unity.Transforms.LocalTransform;
using Random = Unity.Mathematics.Random;

namespace Enemies.AI
{
    class BaseEnemyAuthor : BaseAuthor
    {
        [Header("Enemy Settings")]
        public float health = 10;

        public float size = 10;

        public GameObject intel;
        
        [Header("Linear Settings")]
        public PID.Params linearPid = new() {kd=.5f, ki = .1f, kp = 4};
        
        [Header("Angular Settings")]
        public PID.Params angularPid = new() {kd=.5f, ki = .1f, kp = 4};
        
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref PID.Params linear = ref builder.ConstructRoot<PID.Params>();
            linear = linearPid;
            var linearBlob = builder.CreateBlobAssetReference<PID.Params>(Allocator.Persistent);
            builder.Dispose();
            baker.AddBlobAsset(ref linearBlob, out var hash1);

            builder = new BlobBuilder(Allocator.Temp);
            ref PID.Params angular = ref builder.ConstructRoot<PID.Params>();
            angular = angularPid;
            var angularBlob = builder.CreateBlobAssetReference<PID.Params>(Allocator.Persistent);
            builder.Dispose();
            baker.AddBlobAsset(ref angularBlob, out var hash2);
            
            var singles = GetComponentsInChildren<ThrusterAuthoring>();
            var pairs = GetComponentsInChildren<ThrusterPairAuthoring>();
            builder = new BlobBuilder(Allocator.Temp);
            ref ThrusterBlob hobbyPool = ref builder.ConstructRoot<ThrusterBlob>();
            
            BlobBuilderArray<Thruster> singlesBuilder = builder.Allocate(
                ref hobbyPool.Singles,
                singles.Length
            );
            for (int i = 0; i < singles.Length; i++) singlesBuilder[i] = singles[i].GetData();
            
            BlobBuilderArray<ThrusterPair> pairsBuilder = builder.Allocate(
                ref hobbyPool.Pairs,
                pairs.Length
            );
            for (int i = 0; i < pairs.Length; i++) pairsBuilder[i] = pairs[i].GetData();
            
            var thrusters = builder.CreateBlobAssetReference<ThrusterBlob>(Allocator.Persistent);
            
            baker.AddComponent(entity, new EnemyStats
            {
                Health = health,
                MaxHealth = health,
                IntelPrefab = baker.ToEntity(intel),
                Size = size,
            });
            baker.AddComponent(entity, new EnemyMovement
            {
                LinearPidBlob = linearBlob,
                AngularPidBlob = angularBlob,
                Thrusters = thrusters
            });
            
        }
    }
    
    public struct EnemyStats : IComponentData
    {
        public float Health, MaxHealth, Size;
        public bool Invulnerable;
        public Entity IntelPrefab;
    }

    public struct ThrusterBlob
    {
        public BlobArray<Thruster> Singles;
        public BlobArray<ThrusterPair> Pairs;
    }
    
    public struct EnemyMovement : IComponentData 
    {
        public float3 TargetMoveVel;
        public float3 TargetFaceDir;
        public float3 TargetUpDir;
        
        public BlobAssetReference<PID.Params> LinearPidBlob;
        public BlobAssetReference<PID.Params> AngularPidBlob;
        
        public PID LinearPid;
        public PID AngularPid;

        public BlobAssetReference<ThrusterBlob> Thrusters;
        public ref BlobArray<Thruster> MainThrusters => ref Thrusters.Value.Singles;
        public ref BlobArray<ThrusterPair> SideThrusters => ref Thrusters.Value.Pairs;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct BaseEnemyAI : ISystem
    {
        private BufferLookup<Thruster> _tBuffer;
        private BufferLookup<ThrusterPair> _tPBbuffer;
        private ComponentLookup<LocalTransform> _localTransform;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { 
            state.InitSystemRng("BaseEnemyAI");
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _tBuffer = state.GetBufferLookup<Thruster>();
            _tPBbuffer = state.GetBufferLookup<ThrusterPair>();
            _localTransform = state.GetComponentLookup<LocalTransform>(true);
        }

        public void OnDestroy(ref SystemState state) { }
    
        [BurstCompile]
        public partial struct BaseEnemyMoveJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public Dimension Dim;
                
            [ReadOnly] public BufferLookup<Thruster> MainThrusters;
            [ReadOnly] public BufferLookup<ThrusterPair> Stabilizers;
            
            private void DoStuff(Entity e, ref LocalTransform transform, ref EnemyMovement enemy, PhysicsMass mass, ref PhysicsVelocity physicsVelocity)
            { 
                var moveDir = transform.InverseTransformDirection(enemy.TargetMoveVel);
                var lookDir = transform.InverseTransformDirection(enemy.TargetFaceDir);
                var upDir = transform.InverseTransformDirection(enemy.TargetUpDir);
                
                //float3 targetMoveDir = math.normalize(moveDir);
                float3 targetLookDir = math.normalize(lookDir);
                float3 targetUpDir = math.normalize(upDir);
                
                float3 velocity = transform.InverseTransformDirection(physicsVelocity.Linear);
                
                // Decompose velocity into desired and undesired components
                // float speedTowardsTarget = math.clamp(math.dot(velocity, targetMoveDir), 0, enemy.BaseTargetSpeed);
                //float3 desiredVelocity = speedTowardsTarget * targetMoveDir;
                //float3 unwantedVelocity = velocity - desiredVelocity;
                
                //float3 correctionThrustLocal = -unwantedVelocity * enemy.BaseCorrectWeight;
                
                float3 targetThrust = enemy.LinearPid.Cycle(enemy.LinearPidBlob.Value, velocity, moveDir, DeltaTime);
                
                float3 totalThrust = float3.zero;
                
                for (int i = 0; i < enemy.SideThrusters.Length; i++)
                {
                    float3 t = enemy.SideThrusters[i].ApplyOptimalThrust(targetThrust);
                    totalThrust += t;
                    targetThrust -= t;
                }
                
                for (int i = 0; i < enemy.MainThrusters.Length; i++)
                {
                    float3 t = enemy.MainThrusters[i].ApplyThrust(targetThrust);
                    totalThrust += t;
                    targetThrust -= t;
                }

                physicsVelocity.Linear += transform.TransformDirection(totalThrust) * DeltaTime;
                {
                    var targetAngularVelocity = math.cross(math.forward(), targetLookDir * 10);

                    if (math.all(targetUpDir == float3.zero))
                    {
                        targetAngularVelocity.z = 0;
                    }
                    else
                    {
                        targetAngularVelocity.z = math.cross(math.up(), targetUpDir).z;
                    }
                    
                    var curAngularVelocity = physicsVelocity.Angular;               
                    
                    physicsVelocity.Angular += DeltaTime * enemy.AngularPid.Cycle(enemy.AngularPidBlob.Value, curAngularVelocity, targetAngularVelocity, DeltaTime);;
                }
                if (Dim != Dimension.Three)
                {
                    var r = math.Euler(transform.Rotation);
                    r.x = 0;
                    r.z = 0;
                    transform.Rotation = quaternion.Euler(r);
                }
            }
            
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity enemy, PhysicsMass mass,
                ref EnemyMovement enemyStats, 
                ref LocalTransform transform,
                ref PhysicsVelocity physicsVelocity)
            {
                DoStuff(enemy, ref transform, ref enemyStats, mass, ref physicsVelocity);
            }
        }
        
        [BurstCompile]
        public partial struct BaseEnemyHealthJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            public SystemRng Rng;
            
            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                Rng.BeginChunk(unfilteredChunkIndex);
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask,
                bool chunkWasExecuted)
            {
            }

            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity enemy, EnemyStats enemyStats)
            {
                if (enemyStats.Health <= 0)
                {
                    var transform = TransformLookup[enemy];
                    var intel = Ecb.Instantiate(chunkIndex, enemyStats.IntelPrefab);
                    var s = TransformLookup[enemyStats.IntelPrefab];
                    Ecb.SetComponent(chunkIndex, intel, LocalTransform.FromPositionRotationScale(transform.Position, transform.Rotation, s.Scale));
                    Ecb.SetComponent(chunkIndex, intel, new PhysicsVelocity{Linear = Rng.NextFloat3Direction() * 5});
                    Ecb.DestroyEntity(chunkIndex, enemy);
                }
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _tBuffer.Update(ref state);
            _tPBbuffer.Update(ref state);
            
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            var d1 = new BaseEnemyMoveJob
            {
                MainThrusters = _tBuffer,
                Stabilizers = _tPBbuffer,
                Ecb = ecb,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
            
            d1.Complete();
            
            _localTransform.Update(ref state);
            ecb = GetEntityCommandBuffer(ref state);
            var d2 = new BaseEnemyHealthJob
            {
                Ecb = ecb,
                TransformLookup = _localTransform,
                Rng = state.GetJobRng(),
            }.ScheduleParallel(state.Dependency);
            state.Dependency = d2;
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}