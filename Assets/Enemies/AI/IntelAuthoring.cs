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
            baker.AddComponent(entity, new Intel{BaseValue = value});
            baker.AddComponent(entity, new Lifetime{Time = 120});
        }
    }
    
    public struct Intel : IComponentData
    {
        public float BaseValue;
    }
    
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct IntelSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PlayerData>();
        }
    
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var job = new IntelJob { Dim3 = DimensionManager.burstDim.Data == Dimension.Three};
            state.Dependency = job.ScheduleParallel(state.Dependency);
            state.Dependency.Complete();
        }
        
        [BurstCompile]
        partial struct IntelJob : IJobEntity
        {
            public bool Dim3;
            public void Execute(ref Intel intel, ref LocalTransform transform)
            {
                if (Dim3)
                {
                    var r = math.Euler(transform.Rotation);
                    transform.Rotation = quaternion.Euler(0, r.y, 0);
                }
            }
        }
    }
}