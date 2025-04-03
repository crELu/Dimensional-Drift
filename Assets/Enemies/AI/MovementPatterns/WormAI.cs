using System.Collections.Generic;
using Enemies.Worm;
using Latios.Mecanim;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    public class WormAI : BaseEnemyAuthor
    {
        [Header("Worm Settings")] public GameObject bodyPrefab;
        public Transform bodyContainer;
        public int count;
        public float turnSpeed, moveWeight, orbitWeight, rollWeight;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new WormHead
            {
                TurnSpeed = turnSpeed,
                MoveWeight = moveWeight,
                OrbitWeight = orbitWeight,
                RollWeight = rollWeight
            });
            var attached = baker.AddBuffer<LinkedEntityGroup>(entity);
            attached.Add(entity);
            foreach (var worm in GetComponentsInChildren<WormBodyAuthor>())
            {
                attached.Add(baker.GetEntity(worm, TransformUsageFlags.Dynamic));
            }
            base.Bake(baker, entity);
        }
    }
    
    public struct WormHead : IComponentData
    {
        public float TurnSpeed;
        public float MoveWeight;
        public float OrbitWeight;
        public float RollWeight;
    }
    
    public struct WormBody : IComponentData
    {
        public Entity Head;
        public Entity Prev;
        public float Spacing, Speed, RollSpeed, DamageMultiplier;
    }
    
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(BaseEnemyAI))]
    public partial struct WormAISystem : ISystem
    {
        private ComponentLookup<EnemyCollisionReceiver> _damageReceiverLookup;
        private ComponentLookup<LocalTransform> _localTransformLookup;
        public void OnCreate(ref SystemState state)
        {
            _damageReceiverLookup = state.GetComponentLookup<EnemyCollisionReceiver>();
            _localTransformLookup = state.GetComponentLookup<LocalTransform>();
        }

        public void OnDestroy(ref SystemState state) { }
        
        [BurstCompile]
        private partial struct WormHeadAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;

            private void Execute([ChunkIndexInQuery] int chunkIndex, in LocalTransform transform,
                ref EnemyMovement movement, ref WormHead worm, ref EnemyStats stats, ref PhysicsVelocity velocity)
            {
                var pPos = MathsBurst.DimSwitcher(PlayerPosition, Dim == Dimension.Three);
                var ePos = MathsBurst.DimSwitcher(transform.Position, Dim == Dimension.Three);
                var forw = MathsBurst.DimSwitcher(transform.Forward(), Dim == Dimension.Three);
                
                float3 toTarget = pPos - ePos;
                float3 radialDir = math.normalize(toTarget);
                float3 tDir = math.normalize(math.cross(radialDir, new float3(0, 1, 0))); // One orbit direction
                
                // Project current direction onto tangential candidates
                float dot1 = math.dot(velocity.Linear, tDir);
                float dot2 = math.dot(velocity.Linear, -tDir);

                // Choose the closer direction
                var a = dot1 > dot2 ? tDir : -tDir;
                
                float3 idealPosition = pPos - radialDir * 25;
                float3 correctionDir = math.normalize(idealPosition - ePos);

                movement.TargetMoveVel = MathsBurst.DimSwitcher(forw, Dim == Dimension.Three) * worm.MoveWeight;
                movement.TargetFaceDir = MathsBurst.RotateVectorTowards(forw, toTarget, worm.TurnSpeed * DeltaTime);
                
                quaternion rotation = quaternion.AxisAngle(transform.Forward(), math.radians(worm.RollWeight * DeltaTime));
                movement.TargetUpDir = math.mul(rotation, transform.Up());
                if (math.length(toTarget) < 20 && math.dot(toTarget, forw) > .8f)
                {
                    velocity.Linear += transform.Forward() * 20 * DeltaTime;
                }
            }
        }
        
        [BurstCompile]
        private partial struct WormBodyAIJob : IJobEntity
        {
            public float3 PlayerPosition;
            public float DeltaTime;
            public Dimension Dim;
            public EntityCommandBuffer.ParallelWriter Ecb;
            [NativeDisableParallelForRestriction] public ComponentLookup<EnemyCollisionReceiver> DamageLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref WormBody body, ref PhysicsVelocity velocity)
            {
                var bodyDamage = DamageLookup[entity];
                if (!DamageLookup.EntityExists(body.Head))
                {
                    Ecb.DestroyEntity(chunkIndex, entity);
                    return;
                }
                var headDamage = DamageLookup[body.Head];
                headDamage.LastDamage += bodyDamage.LastDamage * body.DamageMultiplier;
                DamageLookup.GetRefRW(body.Head).ValueRW = headDamage;
                bodyDamage.LastDamage = 0;
                DamageLookup.GetRefRW(entity).ValueRW = bodyDamage;
                
                velocity.Linear = float3.zero;
                velocity.Angular = float3.zero;
                
                var bodyTransform = TransformLookup[entity];
                var prevTransform = TransformLookup[body.Prev];

                var v = math.normalizesafe( bodyTransform.Position - prevTransform.Position);
                var ideal = prevTransform.Position + v * body.Spacing;
                var vel = math.length(ideal - bodyTransform.Position);
                if (vel > 50f)
                {
                    bodyTransform.Position = ideal;
                }
                else
                {
                    bodyTransform.Position = math.lerp(bodyTransform.Position, ideal, body.Speed) ;
                }

                float3 idealUp = prevTransform.Up();
                float3 newUp = math.normalize(math.lerp(bodyTransform.Up(), idealUp, math.clamp(vel/body.RollSpeed, 0, 1)));
                float3 right = math.normalize(math.cross(newUp, -v));
                newUp = math.cross(-v, right);
                
                bodyTransform.Rotation = quaternion.LookRotationSafe(-v, newUp);
                
                Ecb.SetComponent(chunkIndex, entity, bodyTransform);
                
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var bad = SystemAPI.QueryBuilder().WithAll<WormHead>().WithAll<Child>().Build();
            state.EntityManager.RemoveComponent<Child>(bad);
            var badp = SystemAPI.QueryBuilder().WithAll<WormBody>().WithAll<Parent>().Build();
            state.EntityManager.RemoveComponent<Parent>(badp);
            
            state.Dependency = new WormHeadAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data.Position,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
            
            _localTransformLookup.Update(ref state);
            _damageReceiverLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbWriter = ecb.AsParallelWriter();
            
            state.Dependency = new WormBodyAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data.Position,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
                Ecb = ecbWriter,
                DamageLookup = _damageReceiverLookup,
                TransformLookup = _localTransformLookup,
            }.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            
        }
    }
}