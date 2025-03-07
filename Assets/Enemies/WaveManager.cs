using System.Collections.Generic;
using Enemies.AI;
using Latios;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms;

namespace Enemies
{
    public class WaveManager : BaseAuthor
    {
        public List<GameObject> enemies;
        public int difficulty;
        public GameObject yuumi;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new WaveSingleton
            {
                Difficulty=difficulty,
                CatPrefab = baker.ToEntity(yuumi),
            });
            var buffer = baker.AddBuffer<WaveEnemy>(entity);
            for (int i = 0; i < enemies.Count; i++)
            {
                buffer.Add(new WaveEnemy
                {
                    Prefab = baker.ToEntity(enemies[i])
                });
            }
            base.Bake(baker, entity);
        }
    }
    
    public struct WaveSingleton : IComponentData
    {
        public int Difficulty;
        public int Wave;
        public float WaveTimer;
        public Entity CatPrefab;
    }
    
    public struct WaveEnemy : IBufferElementData
    {
        public Entity Prefab;
    }

    //[BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct WaveManagerSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private ComponentLookup<EnemyStats> _enemyStatsLookup;
        private EntityQuery _enemies;
        private Rng _rng;
        
        public void OnCreate(ref SystemState state)
        {
            _rng = new Rng("WaveManagerSystem");
            _enemies = state.GetEntityQuery(ComponentType.ReadOnly<EnemyStats>());
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
            _enemyStatsLookup = state.GetComponentLookup<EnemyStats>(isReadOnly: true);
        }
 
        public void OnDestroy(ref SystemState state) { }
    
        //[BurstCompile]
        private struct WaveJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public NativeArray<int> EntitiesToSpawn;
            public int Radius;
            public Entity YuumiPrefab;
            public float CatRate;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<EnemyStats> EnemyStats;
            [ReadOnly] public DynamicBuffer<WaveEnemy> Enemies;
            public Rng Rng;
            
            public void Execute(int jobIndex)
            {
                var random = Rng.GetSequence(jobIndex);
                var position = random.NextFloat3Direction() * Radius;
                var xz = math.normalize(position.xz) * Radius;
                position.xz = xz;
                var i = EntitiesToSpawn[jobIndex];
                
                var entity = Ecb.Instantiate(jobIndex, Enemies[i].Prefab);

                if (random.NextFloat(0, 1) < CatRate) {
                    var stats = EnemyStats[Enemies[i].Prefab];
                    var catStats = EnemyStats[YuumiPrefab];
                    var cat = Ecb.Instantiate(jobIndex, YuumiPrefab);
                    Ecb.AddComponent(jobIndex, cat, new Yuumi { Attached = entity });
                    float hp = stats.Health * catStats.Health;
                    Ecb.AddComponent(jobIndex, cat,
                        new EnemyStats { IntelPrefab = catStats.IntelPrefab, MaxHealth = hp, Health = hp });
                    Ecb.AddComponent(jobIndex, cat, LocalTransform.FromScale(stats.Size));

                    stats.Invulnerable = true;
                    Ecb.SetComponent(jobIndex, entity, stats);
                }
                
                var originalTransform = LocalTransformLookup.GetRefRO(Enemies[i].Prefab).ValueRO;
                Ecb.SetComponent(jobIndex, entity, new LocalTransform{Position = position, Rotation = quaternion.identity, Scale = originalTransform.Scale});
            }
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localTransformLookup.Update(ref state);
            _enemyStatsLookup.Update(ref state);
            if (SystemAPI.TryGetSingletonEntity<WaveSingleton>(out Entity e))
            {
                bool waveEarlyDone = _enemies.IsEmpty;
                var wave = SystemAPI.GetComponent<WaveSingleton>(e);
                wave.WaveTimer -= SystemAPI.Time.DeltaTime;
                
                PlayerManager.waveTimer = wave.WaveTimer;
                
                if (waveEarlyDone || wave.WaveTimer < 0)
                {
                    wave.Wave++;
                    var count = wave.Difficulty * 10 * wave.Wave;
                    
                    wave.WaveTimer = GetWaveTimer(wave.Wave);
                    PlayerManager.maxWaveTimer = wave.WaveTimer;
                    var enemiesToSpawn = new NativeArray<int>(count, Allocator.TempJob);
                    
                    var enemyPrefabs = SystemAPI.GetBuffer<WaveEnemy>(e);
                    
                    var random = _rng.GetSequence(0);

                    float[] weights = { 20, 4, 2, 1};
                    float totalWeight = 0;
                    
                    foreach (var t in weights)
                        totalWeight += t;

                    for (int i = 0; i < count; i++)
                    {
                        enemiesToSpawn[i] = MathsBurst.ChooseWeightedRandom(ref random, weights, totalWeight);
                    }

                    var ecb = GetEntityCommandBuffer(ref state);
                    var job = new WaveJob
                    {
                        Ecb = ecb.AsParallelWriter(),
                        Enemies = enemyPrefabs,
                        EntitiesToSpawn = enemiesToSpawn,
                        YuumiPrefab = wave.CatPrefab,
                        CatRate = GetCatRate(wave.Wave),
                        LocalTransformLookup = _localTransformLookup,
                        EnemyStats = _enemyStatsLookup,
                        Radius = 1000,
                        Rng = _rng,
                    };

                    job.Schedule(enemiesToSpawn.Length, 16, state.Dependency).Complete();
                }
                
                SystemAPI.SetComponent(e, wave);
            }
        }

        private float GetWaveTimer(int wave)
        {
            return 120;
        }
        
        private float GetCatRate(int wave)
        {
            return 1 - math.pow(.8f, wave);
        }

        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb;
        }
    }
}