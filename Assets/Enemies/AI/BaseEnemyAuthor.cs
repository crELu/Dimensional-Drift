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
        public float baseMovementSpeed;
        public float baseMoveWeight = 30;
        public float baseCorrectWeight = 5;
        
        public float3 baseRotationSpeed = new(1);
        public float baseSpinSpeed = 180;
        public PID angularPid = new() {Kd=new float3(.5f), Ki = new float3(.1f), Kp = new float3(4)};
        
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            angularPid.SetBounds(baseRotationSpeed);
            
            baker.AddComponent(entity, new EnemyStats
            {
                Health = health,
                MaxHealth = health,
                RandomSeed = new Random((uint)UnityEngine.Random.Range(1, 50000)),
                TargetPosition = float3.zero, // transform.position,
                BaseMovementSpeed = baseMovementSpeed,
                BaseMoveWeight = baseMoveWeight,
                BaseSpinSpeed = baseSpinSpeed,
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
        public Random RandomSeed;
        public float3 TargetPosition;
        public float BaseSpinSpeed;
        public float BaseMovementSpeed;
        public float BaseMoveWeight, BaseCorrectWeight;
        public PID AngularPid;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial struct BaseEnemyAI : ISystem
    {
        private BufferLookup<Thruster> _tBuffer;
        private BufferLookup<ThrusterPair> _tPBbuffer;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
            _tBuffer = state.GetBufferLookup<Thruster>(true);
            _tPBbuffer = state.GetBufferLookup<ThrusterPair>(true);
        }

        public void OnDestroy(ref SystemState state) { }
    
        // [BurstCompile]
        public partial struct BaseEnemyAIJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            public float DeltaTime;
            public float3 Pos;
            
            [ReadOnly] public BufferLookup<Thruster> MainThrusters;
            [ReadOnly] public BufferLookup<ThrusterPair> Stabilizers;
            private float3 ComputeCurveNormal(float3 velocity)
            {
                // Estimate curvature using the velocity change (assumes smooth paths)
                float3 perp = math.cross(velocity, new float3(0, 1, 0)); // Assume Y-up world
                return math.normalize(perp);
            }
            
            private void DoStuff(Entity e, ref LocalTransform transform, ref EnemyStats enemy, PhysicsMass mass, ref PhysicsVelocity physicsVelocity, float3 dir)
            { 
                dir = transform.InverseTransformDirection(dir);
                float3 targetDirection = math.normalize(dir);
                float3 velocity = transform.InverseTransformDirection(physicsVelocity.Linear);
                
                // Decompose velocity into desired and undesired components
                float speedTowardsTarget = math.clamp(math.dot(velocity, targetDirection), 0, enemy.BaseMovementSpeed);
                float3 desiredVelocity = speedTowardsTarget * targetDirection;
                float3 unwantedVelocity = velocity - desiredVelocity;
                
                float3 correctionThrustLocal = -unwantedVelocity * enemy.BaseCorrectWeight;

                float3 targetThrust = targetDirection * enemy.BaseMoveWeight + correctionThrustLocal;
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
                    var targetAngularVelocity = math.cross(math.forward(), targetDirection * 10);
                    targetAngularVelocity.z = 1;
                    var curAngularVelocity = physicsVelocity.Angular;               
               
                    enemy.AngularPid.Cycle(curAngularVelocity, targetAngularVelocity, DeltaTime);
                    physicsVelocity.Angular += enemy.AngularPid.output * DeltaTime;
                }

            }
            
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity enemy, PhysicsMass mass,
                ref EnemyStats enemyStats, 
                ref LocalTransform transform,
                ref PhysicsVelocity physicsVelocity)
            {
                if (enemyStats.Health <= 0)
                {
                    Ecb.DestroyEntity(chunkIndex, enemy);
                }
                
                float distance = math.distance(transform.Position, Pos);
                if (distance > Mathf.Epsilon)
                {
                    var idealD = math.normalize(Pos - transform.Position);
                    var smoothD = MathsBurst.RotateVectorTowards(transform.Forward(), idealD, enemyStats.BaseSpinSpeed * DeltaTime);
                    DoStuff(enemy, ref transform, ref enemyStats, mass, ref physicsVelocity, smoothD);
                }
            }
        }

        public void OnUpdate(ref SystemState state)
        {
            _tBuffer.Update(ref state);
            _tPBbuffer.Update(ref state);
            EntityCommandBuffer.ParallelWriter ecb = GetEntityCommandBuffer(ref state);
            new BaseEnemyAIJob
            {
                MainThrusters = _tBuffer,
                Stabilizers = _tPBbuffer,
                Ecb = ecb,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Pos = PlayerManager.Position
            }.ScheduleParallel();
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}