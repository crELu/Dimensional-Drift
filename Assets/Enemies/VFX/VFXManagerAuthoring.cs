using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct GraphicsReceiver : IComponentData
{
    public NativeQueue<OneShotData> AudioCommands;
}

[UpdateBefore(typeof(PhysicsSystem))]
public partial struct VfxProcessSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<GraphicsReceiver>();
        state.EntityManager.CreateSingleton(new GraphicsReceiver{AudioCommands = new NativeQueue<OneShotData>(Allocator.Persistent)});
    }

    public void OnDestroy(ref SystemState state) { }
    
    public void OnUpdate(ref SystemState state)
    {
        if (!VFXManager.main) return;
        var graphicsReceiver = SystemAPI.GetSingleton<GraphicsReceiver>();
        var graphicsWriters = graphicsReceiver.AudioCommands;
        
        var a = graphicsWriters.ToArray(Allocator.Temp);
        VFXManager.main.PlayOneShot(a);
        a.Dispose();
        graphicsWriters.Clear();
    }

}