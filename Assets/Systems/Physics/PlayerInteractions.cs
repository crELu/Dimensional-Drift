
using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public struct PlayerInteractions : IFindPairsProcessor {
    public PhysicsComponentLookups ComponentLookups;
    public EntityCommandBuffer.ParallelWriter Ecb;
    public NativeParallelHashSet<Entity>.ParallelWriter DestroyedSetWriter;
    
    public void Execute(in FindPairsResult result) {
        ColliderDistanceResult r;
        if (Physics.DistanceBetween(
                    result.bodyA.collider, result.bodyA.transform,
                    result.bodyB.collider, result.bodyB.transform,
                    0, out r))
        {
            Calculate(result.entityA, result.entityB);
        }
    }
    
    [BurstCompile]
    private void Calculate(SafeEntity playerEntity, SafeEntity entityB)
    {
        PlayerData player = ComponentLookups.PlayerLookup.GetRW(playerEntity).ValueRW;
        
        if (ComponentLookups.IntelLookup.HasComponent(entityB))
        {
            var playerPos = ComponentLookups.transform.GetRW(playerEntity).ValueRW;
            var intelPos = ComponentLookups.transform.GetRW(entityB).ValueRW;
            var intelVel = ComponentLookups.velocity.GetRW(entityB).ValueRW;
            intelVel.Linear = math.normalize(playerPos.Position - intelPos.Position) * 10;
            ComponentLookups.velocity.GetRW(entityB).ValueRW = intelVel;
        }
    }
}