using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


[BurstCompile]
public struct PairsProcessor: IFindPairsProcessor {
    public PhysicsComponentLookups ComponentLookups;
    public float DeltaTime;
    public Dimension Dim;
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
            Calculate(result.entityB, result.entityA);
        }
    }
    
    [BurstCompile]
    private void Calculate(SafeEntity entityA, SafeEntity entityB)
    {
        var eA = ComponentLookups.EnemyLookup.GetRW(entityA).ValueRW;
        var eB = ComponentLookups.EnemyLookup.GetRW(entityB).ValueRW;
        
        var tA = ComponentLookups.transform.GetRW(entityA).ValueRW;
        var tB = ComponentLookups.transform.GetRW(entityB).ValueRW;
        var vA = ComponentLookups.velocity.GetRW(entityA).ValueRW;
        var vB = ComponentLookups.velocity.GetRW(entityB).ValueRW;
        
        var d = tA.Position - tB.Position;
        if (Dim == Dimension.Two) d.y = 0;
        var massRatio = eA.Size * eA.Size / (eA.Size * eA.Size + eB.Size * eB.Size);
        vA.Linear += DeltaTime * math.normalize(d) * 15 * massRatio;
        vB.Linear += DeltaTime * -math.normalize(d) * 15 / massRatio;

        ComponentLookups.velocity.GetRW(entityA).ValueRW = vA;
        ComponentLookups.velocity.GetRW(entityB).ValueRW = vB;
    }
}