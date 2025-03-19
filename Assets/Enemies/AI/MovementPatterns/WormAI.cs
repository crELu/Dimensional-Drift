using Latios.Mecanim;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Enemies.AI
{
    class WormAI : BaseEnemyAuthor
    {
        [Header("Worm Settings")] public int count;
        public GameObject bodyPrefab;
        public float spacing;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new WormHead
            {
                
            });
            baker.AddComponent(entity, new TempWormTag
            {
                Prefab = baker.ToEntity(bodyPrefab),
                Count = count,
            });
            // var linked = baker.AddBuffer<LinkedEntityGroup>(entity);
            // linked.Capacity = count;
            // var prev = entity;
            // for (int i = 0; i < count; i++)
            // {
            //     var body = baker.ToEntity(bodyPrefab);
            //     linked.Add(body);
            //     baker.AddComponent(body, new WormBody{Prev = prev, Spacing = spacing});
            //     baker.AddComponent(body, LocalTransform.FromPosition(transform.position + new Vector3(0, 0, spacing * i)));
            //     prev = body;
            // }
            base.Bake(baker, entity);
        }
    }
    
    public struct WormHead : IComponentData
    {
        public float A;
    }
    
    public struct WormBody : IComponentData
    {
        public Entity Head;
        public Entity Prev;
        public float Spacing;
    }

    public struct TempWormTag : IComponentData
    {
        public Entity Prefab;
        public int Count;
    }

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    partial struct AddTagToRotationBakingSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var queryMissingTag = SystemAPI.QueryBuilder()
                .WithNone<TempWormTag>()
                .Build();

            state.EntityManager.AddComponent<TempWormTag>(queryMissingTag);

            // Omitting the second part of this function would lead to inconsistent
            // results during live baking. Added tags would remain on the entity even
            // after removing the RotationSpeed component.

            var queryCleanupTag = SystemAPI.QueryBuilder()
                .WithAll<TempWormTag>()
                .Build();

            state.EntityManager.RemoveComponent<TempWormTag>(queryCleanupTag);
        }
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
                ref EnemyMovement movement, ref WormHead worm, MecanimAspect animator, ref EnemyStats stats)
            {
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
            private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, ref WormBody body, ref DynamicBuffer<EnemyShoot> guns)
            {
                var bodyDamage = DamageLookup[entity];
                var headDamage = DamageLookup[body.Head];
                headDamage.LastDamage += bodyDamage.LastDamage;
                DamageLookup.GetRefRW(entity).ValueRW = headDamage;
                bodyDamage.LastDamage = 0;
                DamageLookup.GetRefRW(entity).ValueRW = bodyDamage;
                
                var bodyTransform = TransformLookup[entity];
                var prevTransform = TransformLookup[body.Prev];

                var v = math.normalizesafe(prevTransform.Position - bodyTransform.Position);
                bodyTransform.Position = prevTransform.Position + v * body.Spacing;
                Ecb.SetComponent(chunkIndex, entity, bodyTransform);
            }
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new WormHeadAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
            }.ScheduleParallel(state.Dependency);
            
            _localTransformLookup.Update(ref state);
            _damageReceiverLookup.Update(ref state);
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var ecbWriter = ecb.AsParallelWriter();
            state.Dependency = new WormBodyAIJob
            {
                PlayerPosition = PlayerManager.burstPos.Data,
                DeltaTime = SystemAPI.Time.fixedDeltaTime,
                Dim = DimensionManager.burstDim.Data,
                Ecb = ecbWriter,
                DamageLookup = _damageReceiverLookup,
                TransformLookup = _localTransformLookup,
            }.ScheduleParallel(state.Dependency);
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            
        }
    }
}