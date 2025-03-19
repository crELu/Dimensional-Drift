
using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public struct PlayerPairs: IFindPairsProcessor {
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
            
        }
        else if (ComponentLookups.EnemyWeaponLookup.HasComponent(entityB))
        {
            DamagePlayer enemyProj = ComponentLookups.EnemyWeaponLookup.GetRW(entityB).ValueRW;
            player.LastDamage += enemyProj.Damage;
            ComponentLookups.PlayerLookup.GetRW(playerEntity).ValueRW = player;
            if (enemyProj.DieOnHit)
            {
                DestroyedSetWriter.Add(entityB);
            }
        }
        else if (ComponentLookups.TerrainLookup.HasComponent(entityB))
        {
            
        }
    }
}