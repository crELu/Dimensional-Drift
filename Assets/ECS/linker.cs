using Unity.Entities;
using Unity.Transforms;

public partial class LinkChildrenSystem : SystemBase {
   
    private EndSimulationEntityCommandBufferSystem _endSimECBSystem;
   
    protected override void OnCreate()
    {
        _endSimECBSystem = World.GetOrCreateSystemManaged<EndSimulationEntityCommandBufferSystem>();
    }
   
    protected override void OnUpdate()
    {
        var ecb = _endSimECBSystem.CreateCommandBuffer();
       
        Entities.WithNone<LinkedEntityGroup>()
            .ForEach(
                (Entity entity, in DynamicBuffer<Child> children) =>
                {
                    DynamicBuffer<LinkedEntityGroup> group = ecb.AddBuffer<LinkedEntityGroup>(entity);
                    group.Add(entity);  // Always add self as first member of group.
                    foreach (Child child in children)
                    {
                        group.Add(child.Value);
                    }
                })
            .Schedule();

        _endSimECBSystem.AddJobHandleForProducer(Dependency);
    }
    private EntityCommandBuffer.ParallelWriter GetEntityCommandBuffer(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        return ecb.AsParallelWriter();
    }
}