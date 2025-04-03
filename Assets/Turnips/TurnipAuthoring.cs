using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Turnips
{
    public class TurnipAuthoring : BaseAuthor
    {
        public override void Bake(UniversalBaker baker, Entity entity)
        {
            baker.AddComponent(entity, new TurnipTag());
        }
    }

    public struct TurnipTag : IComponentData { }
    
    
    public partial struct TurnipSystem : ISystem
    {
        public void OnCreate(ref SystemState state) { }

        public void OnDestroy(ref SystemState state) { }
        
        
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (player, transform) in SystemAPI.Query<TurnipTag, RefRW<LocalTransform>>())
            {
                var t = transform.ValueRO;
                if (DimensionManager.burstDim.Data == Dimension.Three)
                {
                    t.Position.y = 0;
                }
                else
                {
                    if (DimensionManager.t == 1) t.Position.y = -1000;
                }

                transform.ValueRW = t;
            }
        }
    }
}