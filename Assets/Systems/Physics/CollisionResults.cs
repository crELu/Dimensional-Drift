using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


[BurstCompile]
public struct PairsProcessor: IFindPairsProcessor {
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
            Calculate(result.entityB, result.entityA);
        }
    }
    
    [BurstCompile]
    private void Calculate(SafeEntity entityA, SafeEntity entityB)
    {
        if (ComponentLookups.EnemyWeaponLookup.HasComponent(entityA) && ComponentLookups.PlayerWeaponLookup.HasComponent(entityB))
        {
            DamagePlayer enemyProj = ComponentLookups.EnemyWeaponLookup.GetRW(entityA).ValueRW;
            PlayerProjectile playerProj = ComponentLookups.PlayerWeaponLookup.GetRW(entityB).ValueRW;
            if (enemyProj.Mass == -1)
            {
                DestroyedSetWriter.Add(entityB);
            }
            else
            {
                while (enemyProj.Mass > 0 && playerProj.Health > 0)
                {
                    enemyProj.Mass--;
                    playerProj.Health -= (int)math.ceil(10000f / ((1+playerProj.Stats.pierce)*(1+playerProj.Stats.power)));
                }
                
                ComponentLookups.EnemyWeaponLookup.GetRW(entityA).ValueRW = enemyProj;
                ComponentLookups.PlayerWeaponLookup.GetRW(entityB).ValueRW = playerProj;
                
                if (enemyProj.Mass == 0)
                {
                    Ecb.DestroyEntity(0, entityA);
                }
                if (playerProj.Health == 0)
                {
                    Ecb.DestroyEntity(0, entityB);
                }
            }
        }
    }
}