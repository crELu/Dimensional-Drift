using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct LaserEffects
{
    public float GrowthRate;
    public int RefractCount;
}

public struct LaserGrowth : IComponentData
{
    public float GrowthRate;
}

public struct LaserRefract : IComponentData
{
    public int RefractCount;
}

[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateBefore(typeof(GunSystem))]
public partial struct LaserSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerData>();
    }

    public void OnUpdate(ref SystemState state)
    {
        Entity playerE = SystemAPI.GetSingletonEntity<PlayerAspect>();
        PlayerAspect player = SystemAPI.GetAspect<PlayerAspect>(playerE);
        var playerTransform = player.Transform;
        var playerData = PlayerManager.main;
        if (LaserWeapon.LaserIsActive)
        {
            foreach (var (laserTransform, _) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<LaserTag>>())
            {
                laserTransform.ValueRW.Position = playerTransform.Position + 
                                                  math.rotate(playerData.transform.rotation, LaserWeapon.LaserOffset);
                laserTransform.ValueRW.Rotation = playerData.movement.LookRotation;
            }
        }
        else
        {
            DespawnLasers(ref state);
        }
    }
    
    [BurstCompile]
    partial struct DespawnLasersJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
    
        public void Execute(Entity entity, [EntityIndexInQuery] int index, in LaserTag laser)
        {
            ECB.DestroyEntity(index, entity);
        }
    }

    private void DespawnLasers(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var job = new DespawnLasersJob { ECB = ecb.AsParallelWriter() };
    
        state.Dependency = job.ScheduleParallel(state.Dependency);
    
        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}