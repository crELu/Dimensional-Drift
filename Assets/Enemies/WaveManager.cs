using System;
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
        // public static AnimationCurve catFreqG, enemyCountG;
        // public static AnimationCurve catFreq, enemyCount;
        public List<WaveEnemy> enemies;
        public int difficulty;
        public GameObject yuumi;
        public AnimationCurve starfishFreq;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponentObject(entity, new WaveSingleton
            {
                Difficulty=difficulty,
                CatPrefab = baker.ToEntity(yuumi),
                StarfishFreq = starfishFreq,
            });
            var buffer = baker.AddBuffer<WaveEnemyData>(entity);
            for (int i = 0; i < enemies.Count; i++)
            {
                buffer.Add(new WaveEnemyData
                {
                    Prefab = baker.ToEntity(enemies[i].prefab),
                    Weight = enemies[i].weight,
                });
            }
            base.Bake(baker, entity);
        }
    }
    
    public class WaveSingleton : IComponentData
    {
        public int Difficulty;
        public int Wave;
        public float WaveTimer;
        public Entity CatPrefab;
        public AnimationCurve StarfishFreq;
    }
    
    [Serializable]
    public struct WaveEnemy
    {
        public GameObject prefab;
        public int weight;
    }
    
    public struct WaveEnemyData : IBufferElementData
    {
        public Entity Prefab;
        public int Weight;
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
            [ReadOnly] public DynamicBuffer<WaveEnemyData> Enemies;
            public Rng Rng;
            
            public void Execute(int jobIndex)
            {
                var random = Rng.GetSequence(jobIndex);
                var position = random.NextFloat3Direction() * Radius;
                var xz = math.normalizesafe(position.xz) * Radius;
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
                    Ecb.AddComponent(jobIndex, cat, new EnemyCollisionReceiver {Size = stats.Size});
                    Ecb.AddComponent(jobIndex, cat, new EnemyGhostedTag());
                    
                    Ecb.AddComponent(jobIndex, cat, LocalTransform.FromScale(stats.Size));
                    Ecb.SetComponent(jobIndex, entity, new EnemyCollisionReceiver{Invulnerable = true, Size = stats.Size});
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
            if (SystemAPI.ManagedAPI.TryGetSingletonEntity<WaveSingleton>(out Entity e))
            {
                bool waveEarlyDone = _enemies.IsEmpty;
                var wave = SystemAPI.ManagedAPI.GetComponent<WaveSingleton>(e);
                wave.WaveTimer -= SystemAPI.Time.DeltaTime * GetWaveSpeed(wave.Wave, _enemies.CalculateEntityCount());
                
                PlayerManager.waveTimer = wave.WaveTimer;
                
                if (waveEarlyDone || wave.WaveTimer < 0)
                {
                    wave.Wave++;
                    
                    PlayerManager.waveCount = wave.Wave;
                    PlayerManager.main.inventory.UpdateUI();
                    PlayerManager.main.StartNewWave(wave.Wave);
                    var count = GetEnemyCount(wave.Wave, wave.Difficulty);
                    
                    wave.WaveTimer = GetWaveTimer(wave.Wave);
                    PlayerManager.maxWaveTimer = wave.WaveTimer;
                    var enemiesToSpawn = new NativeArray<int>(count, Allocator.TempJob);
                    
                    var enemyPrefabs = SystemAPI.GetBuffer<WaveEnemyData>(e);
                    
                    var random = _rng.GetSequence(0);
                    float[] weights = new float[12];
                    for (int i = 0; i < enemyPrefabs.Length; i++)
                    {
                        weights[i] = enemyPrefabs[i].Weight;
                    }
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
                        CatRate = wave.StarfishFreq.Evaluate(wave.Wave),
                        LocalTransformLookup = _localTransformLookup,
                        EnemyStats = _enemyStatsLookup,
                        Radius = 1000,
                        Rng = _rng,
                    };

                    job.Schedule(enemiesToSpawn.Length, 16, state.Dependency).Complete();
                }
                
                state.EntityManager.AddComponentData(e, wave);
            }
        }
        private int GetEnemyCount(int wave, int difficulty)
        {
            return difficulty * wave * wave;
        }
        private float GetWaveTimer(int wave)
        {
            return 240;
        }
        private float GetWaveSpeed(int wave, int enemyCount)
        {
            return enemyCount <= 10 ? 10 - enemyCount : 1;
        }

        private EntityCommandBuffer GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb;
        }
    }
}