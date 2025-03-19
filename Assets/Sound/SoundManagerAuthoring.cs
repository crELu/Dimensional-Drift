using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct SfxCommand
{
    public FixedString64Bytes Name;
    public float3 Position;
}

public struct SoundReceiver : IComponentData
{
    public NativeQueue<SfxCommand> AudioCommands;
}

[UpdateBefore(typeof(PhysicsSystem))]
public partial struct SoundProcessSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.EntityManager.CreateSingleton(new SoundReceiver{AudioCommands = new NativeQueue<SfxCommand>(Allocator.Persistent)});
    }

    public void OnDestroy(ref SystemState state) { }
    
    public void OnUpdate(ref SystemState state)
    {
        var soundReceiver = SystemAPI.GetSingleton<SoundReceiver>();
        var soundWriter = soundReceiver.AudioCommands;
        if (!soundWriter.IsEmpty())
        {
            var a = soundWriter.ToArray(Allocator.Temp);
            SoundManager.main.ProcessAudio(a.ToArray());
            a.Dispose();
        }
        soundWriter.Clear();
    }

}