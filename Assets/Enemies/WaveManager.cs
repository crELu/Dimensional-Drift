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
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new WaveSingleton
            {
                Difficulty=difficulty,
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
    }
    
    public struct WaveEnemy : IBufferElementData
    {
        public Entity Prefab;
    }

    //[BurstCompile]
    public partial struct WaveManagerSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _localTransformLookup;
        private EntityQuery _enemies;
        private Rng _rng;
        
        public void OnCreate(ref SystemState state)
        {
            _rng = new Rng("WaveManagerSystem");
            _enemies = state.GetEntityQuery(ComponentType.ReadOnly<EnemyStats>());
            _localTransformLookup = state.GetComponentLookup<LocalTransform>(isReadOnly: true);
        }

        public void OnDestroy(ref SystemState state) { }
    
        //[BurstCompile]
        private struct WaveJob : IJobParallelFor
        {
            public EntityCommandBuffer.ParallelWriter Ecb;
            [ReadOnly] public NativeArray<int> EntitiesToSpawn;
            public int Radius;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransform;
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
                var originalTransform = LocalTransform.GetRefRO(Enemies[i].Prefab).ValueRO;
                Ecb.SetComponent(jobIndex, entity, new LocalTransform{Position = position, Rotation = quaternion.identity, Scale = originalTransform.Scale});
            }
        }

        //[BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _localTransformLookup.Update(ref state);
            if (SystemAPI.TryGetSingletonEntity<WaveSingleton>(out Entity e))
            {
                bool waveEarlyDone = _enemies.IsEmpty;
                var wave = SystemAPI.GetComponent<WaveSingleton>(e);
                wave.WaveTimer -= SystemAPI.Time.DeltaTime;
                PlayerManager.waveTimer = wave.WaveTimer;
                
                if (waveEarlyDone || wave.WaveTimer < 0)
                {
                    var count = wave.Difficulty * 100;
                    
                    wave.WaveTimer = GetWaveTimer(wave.Wave);
                    PlayerManager.maxWaveTimer = wave.WaveTimer;
                    var enemiesToSpawn = new NativeArray<int>(count, Allocator.TempJob);
                    
                    var enemyPrefabs = SystemAPI.GetBuffer<WaveEnemy>(e);
                    
                    var random = _rng.GetSequence(0);
                    for (int i = 0; i < count; i++)
                    {
                        enemiesToSpawn[i] = random.NextInt(0, 2);
                    }
                    
                    var job = new WaveJob
                    {
                        Ecb = GetEntityCommandBuffer(ref state),
                        Enemies = enemyPrefabs,
                        EntitiesToSpawn = enemiesToSpawn,
                        LocalTransform = _localTransformLookup,
                        Radius = 1000,
                        Rng = _rng,
                    };

                    JobHandle handle = job.Schedule(enemiesToSpawn.Length, 16, state.Dependency);

                    state.Dependency = handle;
                }
                
                SystemAPI.SetComponent(e, wave);
            }
        }

        private float GetWaveTimer(int wave)
        {
            return 120;
        }

        private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
            return ecb.AsParallelWriter();
        }
    }
}