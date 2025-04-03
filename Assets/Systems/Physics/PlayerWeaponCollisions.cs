
using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
public struct PlayerWeaponPairs: IFindPairsProcessor {
    public PhysicsComponentLookups ComponentLookups;
    public EntityCommandBuffer.ParallelWriter Ecb;
    public NativeParallelHashSet<Entity>.ParallelWriter DestroyedSetWriter;
    public NativeQueue<SfxCommand>.ParallelWriter AudioWriter;
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
    private void Calculate(SafeEntity pProj, SafeEntity entityB)
    {
        PlayerProjectile playerProj = ComponentLookups.PlayerWeaponLookup.GetRW(pProj).ValueRW;
        PlayerProjectileDeath playerProjStats = ComponentLookups.PlayerWeaponStatsLookup.GetRW(pProj).ValueRW;
        LocalTransform projPos = ComponentLookups.transform.GetRW(pProj).ValueRW;
        
        if (ComponentLookups.EnemyLookup.HasComponent(entityB)) // Hit Enemy
        {
            EnemyCollisionReceiver enemy = ComponentLookups.EnemyLookup.GetRW(entityB).ValueRW;
            if (!enemy.Invulnerable) enemy.LastDamage += playerProjStats.Stats.damage;

            playerProj.Health -= (int)math.ceil(10000f / (1+playerProjStats.Stats.pierce));
            
            ComponentLookups.EnemyLookup.GetRW(entityB).ValueRW = enemy;
            AudioWriter.Enqueue(new SfxCommand {Name = "Hit Enemy", Position = projPos.Position});
            ComponentLookups.PlayerWeaponLookup.GetRW(pProj).ValueRW = playerProj;
            if (playerProj.Health <= 0)
            {
                DestroyedSetWriter.Add(pProj);
            }
        }
        else if (ComponentLookups.EnemyWeaponLookup.HasComponent(entityB)) // Hit Enemy Projectile
        {
            DamagePlayer enemyProj = ComponentLookups.EnemyWeaponLookup.GetRW(entityB).ValueRW;
            if (enemyProj.Mass == -1)
            {
                DestroyedSetWriter.Add(entityB);
            }
            else
            {
                while (enemyProj.Mass > 0 && playerProj.Health > 0)
                {
                    enemyProj.Mass--;
                    playerProj.Health -= (int)math.ceil(10000f / ((1+playerProjStats.Stats.pierce)*(1+playerProjStats.Stats.power)));
                }
                
                ComponentLookups.EnemyWeaponLookup.GetRW(entityB).ValueRW = enemyProj;
                ComponentLookups.PlayerWeaponLookup.GetRW(pProj).ValueRW = playerProj;
                
                if (enemyProj.Mass <= 0)
                {
                    DestroyedSetWriter.Add(entityB);
                }
                if (playerProj.Health <= 0)
                {
                    DestroyedSetWriter.Add(pProj);
                }
            }
        }
    }
}