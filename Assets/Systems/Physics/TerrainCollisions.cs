
using Enemies.AI;
using Latios.Psyshock;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Physics = Latios.Psyshock.Physics;

[BurstCompile]
public struct TerrainPairs: IFindPairsProcessor {
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
    private void Calculate(SafeEntity terrain, SafeEntity entityB)
    {
        Obstacle obstacle = ComponentLookups.TerrainLookup.GetRW(terrain).ValueRW;
        
        if (ComponentLookups.EnemyLookup.HasComponent(entityB)) // Hit Enemy
        {
            
        }
        if (ComponentLookups.PlayerLookup.HasComponent(entityB)) // Hit Enemy
        {
            
        }
        else if (ComponentLookups.EnemyWeaponLookup.HasComponent(entityB))
        {
            DestroyedSetWriter.Add(entityB);
        }
        else if (ComponentLookups.PlayerWeaponLookup.HasComponent(entityB))
        {
            DestroyedSetWriter.Add(entityB);
        }
    }
}