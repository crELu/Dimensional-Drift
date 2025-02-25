using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
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
        public int health = 10;
        
        [Header("Linear Settings")]
        public float baseMovementSpeed = 5;
        public float baseMoveWeight = 30;
        public float baseCorrectWeight = 5;
        
        [Header("Angular Settings")]
        public float3 baseRotationSpeed = new(1);
        public PID angularPid = new() {Kd=new float3(.5f), Ki = new float3(.1f), Kp = new float3(4)};
        
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            angularPid.SetBounds(baseRotationSpeed);
            
            baker.AddComponent(entity, new EnemyStats
            {
                Health = health,
                MaxHealth = health,
            });
            baker.AddComponent(entity, new EnemyMovement
            {
                BaseMovementSpeed = baseMovementSpeed,
                BaseMoveWeight = baseMoveWeight,
                BaseCorrectWeight = baseCorrectWeight,
                AngularPid = angularPid,
            });
            var control = baker.AddBuffer<ThrusterPair>(entity);
            var main = baker.AddBuffer<Thruster>(entity);
            foreach (var t in GetComponentsInChildren<ThrusterPairAuthoring>())
            {
                control.Add(t.GetData());
            }
            foreach (var t in GetComponentsInChildren<ThrusterAuthoring>())
            {
                main.Add(t.GetData());
            }
        }
    }
    
    public struct EnemyStats : IComponentData 
    {
        public float Health, MaxHealth;
    }
    
    public struct EnemyMovement : IComponentData 
    {
        public float3 TargetMoveDir;
        public float3 TargetFaceDir;
        public float TargetRoll;
        
        public float BaseMovementSpeed;
        public float BaseMoveWeight, BaseCorrectWeight;
        public PID AngularPid;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial struct BaseEnemyAI : ISystem
    {
        private BufferLookup<Thruster> _tBuffer;
        private BufferLookup<ThrusterPair> _tPBbuffer;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _tBuffer = state.GetBufferLookup<Thruster>(true);
            _tPBbuffer = state.GetBufferLookup<ThrusterPair>(true);
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
            private float3 ComputeCurveNormal(float3 velocity)
            {
                // Estimate curvature using the velocity change (assumes smooth paths)
                float3 perp = math.cross(velocity, new float3(0, 1, 0)); // Assume Y-up world
                return math.normalize(perp);
            }
            
            private void DoStuff(Entity e, ref LocalTransform transform, ref EnemyMovement enemy, PhysicsMass mass, ref PhysicsVelocity physicsVelocity, float3 moveDir, float3 lookDir, float roll)
            { 
                moveDir = transform.InverseTransformDirection(moveDir);
                lookDir = transform.InverseTransformDirection(lookDir);
                
                float3 targetMoveDir = math.normalize(moveDir);
                float3 targetLookDir = math.normalize(lookDir);
                
                float3 velocity = transform.InverseTransformDirection(physicsVelocity.Linear);
                
                // Decompose velocity into desired and undesired components
                float speedTowardsTarget = math.clamp(math.dot(velocity, targetMoveDir), 0, enemy.BaseMovementSpeed);
                float3 desiredVelocity = speedTowardsTarget * targetMoveDir;
                float3 unwantedVelocity = velocity - desiredVelocity;
                
                float3 correctionThrustLocal = -unwantedVelocity * enemy.BaseCorrectWeight;

                float3 targetThrust = targetMoveDir * enemy.BaseMoveWeight + correctionThrustLocal;
                float3 totalThrust = float3.zero;
                
                if (Stabilizers.TryGetBuffer(e, out DynamicBuffer<ThrusterPair> pairs))
                {
                    foreach (var pair in pairs)
                    {
                        float3 t = pair.ApplyOptimalThrust(targetThrust);
                        totalThrust += t;
                        targetThrust -= t;
                    }
                }
                
                if (MainThrusters.TryGetBuffer(e, out DynamicBuffer<Thruster> mains))
                {
                    foreach (var main in mains)
                    {
                        float3 t = main.ApplyThrust(targetThrust);
                        totalThrust += t;
                        targetThrust -= t;
                    }
                }

                physicsVelocity.Linear += transform.TransformDirection(totalThrust) * DeltaTime;
                {
                    var targetAngularVelocity = math.cross(math.forward(), targetLookDir * 10);
                    targetAngularVelocity.z = roll;
                    var curAngularVelocity = physicsVelocity.Angular;               
               
                    enemy.AngularPid.Cycle(curAngularVelocity, targetAngularVelocity, DeltaTime);
                    physicsVelocity.Angular += enemy.AngularPid.output * DeltaTime;
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
                DoStuff(enemy, ref transform, ref enemyStats, mass, ref physicsVelocity, enemyStats.TargetMoveDir, enemyStats.TargetFaceDir, enemyStats.TargetRoll);
            }
        }
        
        [BurstCompile]
        public partial struct BaseEnemyHealthJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity enemy, EnemyStats enemyStats)
            {
                if (enemyStats.Health <= 0)
                {
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
            new BaseEnemyMoveJob
            {
                MainThrusters = _tBuffer,
                Stabilizers = _tPBbuffer,
                Ecb = ecb,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel();
            
            ecb = GetEntityCommandBuffer(ref state);
            state.Dependency = new BaseEnemyHealthJob
            {
                Ecb = ecb,
            }.ScheduleParallel(state.Dependency);
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}