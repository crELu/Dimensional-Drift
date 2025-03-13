using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Enemies.AI
{
    public class IntelAuthoring : BaseAuthor
    {
        public float value;
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new Intel{BaseValue = value, Life = 60});
        }
    }
    
    public struct Intel : IComponentData
    {
        public float BaseValue;
        public float Life;
    }
    
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct IntelSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerData>();
        }
    
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.TempJob);
            var job = new IntelJob { ECB = ecb.AsParallelWriter(), DeltaTime = SystemAPI.Time.DeltaTime};
        
            state.Dependency = job.ScheduleParallel(state.Dependency);
        
            state.Dependency.Complete();
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        
        [BurstCompile]
        partial struct IntelJob : IJobEntity
        {
            public float DeltaTime;
            public EntityCommandBuffer.ParallelWriter ECB;
            public void Execute(Entity entity, [ChunkIndexInQuery] int index, ref Intel intel)
            {
                intel.Life -= DeltaTime;
                if (intel.Life <= 0)
                {
                    ECB.DestroyEntity(index, entity);
                }
            }
        }

    }
}