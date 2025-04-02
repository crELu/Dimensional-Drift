using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SfxCommand
{
    public FixedString64Bytes Name;
    public float3 Position;
}

public struct VfxReceiver : IComponentData
{
    public NativeQueue<SfxCommand> VfxCommands;
}

[UpdateBefore(typeof(PhysicsSystem))]
public partial struct SoundProcessSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<VfxReceiver>();
        state.EntityManager.CreateSingleton(new VfxReceiver{VfxCommands = new NativeQueue<SfxCommand>(Allocator.Persistent)});
    }

    public void OnDestroy(ref SystemState state) { }
    
    public void OnUpdate(ref SystemState state)
    {
        var soundReceiver = SystemAPI.GetSingleton<VfxReceiver>();
        var soundWriter = soundReceiver.VfxCommands;
        if (!soundWriter.IsEmpty())
        {
            var a = soundWriter.ToArray(Allocator.Temp);
            SoundManager.main.ProcessAudio(a.ToArray());
            a.Dispose();
        }
        soundWriter.Clear();
    }

}