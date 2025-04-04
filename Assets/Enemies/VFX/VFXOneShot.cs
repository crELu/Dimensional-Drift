using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

public class VFXOneShot: MonoBehaviour
{
    public new string name;
    public int capacity;
    public VisualEffect effectGraph;
    public bool color, scale, duration;
    private GraphicsBuffer _posBuffer, _angleBuffer, _colorBuffer, _durationBuffer, _scaleBuffer;
    private bool _lastActive;
    private void Start()
    {
        _posBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, capacity, 12);
        _angleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, capacity, 12);
        if (color) _colorBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, capacity, 12);
        if (duration) _durationBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, capacity, 4);
        if (scale) _scaleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Raw, capacity, 12);
    }

    public void PlayEffects(List<OneShotData> data)
    {
        if (data.Count == 0 && !_lastActive) return;
        
        _lastActive = data.Count != 0;
        int count = Mathf.Min(data.Count, capacity);
        NativeArray<float3> positions = new NativeArray<float3>(capacity, Allocator.Temp);
        NativeArray<float3> angles = new NativeArray<float3>(capacity, Allocator.Temp);
        NativeArray<float3> colors = color ? new NativeArray<float3>(capacity, Allocator.Temp) : default;
        NativeArray<float3> scales = scale ? new NativeArray<float3>(capacity, Allocator.Temp) : default;
        NativeArray<float> durations = duration ? new NativeArray<float>(capacity, Allocator.Temp) : default;
    
        for (int i = 0; i < count; i++)
        {
            positions[i] = data[i].Position;
            angles[i] = data[i].Angle;
            if (color) colors[i] = data[i].Color;
            if (scale) scales[i] = data[i].Scale;
            if (duration) durations[i] = data[i].Duration;
        }
    
        for (int i = count; i < capacity; i++)
        {
            positions[i] = float3.zero;
            angles[i] = float3.zero;
            if (color) colors[i] = float3.zero;
            if (scale) scales[i] = float3.zero;
            if (duration) durations[i] = 0f;
        }
    
        _posBuffer.SetData(positions);
        _angleBuffer.SetData(angles);
        if (color) _colorBuffer.SetData(colors);
        if (scale) _scaleBuffer.SetData(scales);
        if (duration) _durationBuffer.SetData(durations);
    
        effectGraph.SetGraphicsBuffer("PositionBuffer", _posBuffer);
        effectGraph.SetGraphicsBuffer("AngleBuffer", _angleBuffer);
        if (color) effectGraph.SetGraphicsBuffer("ColorBuffer", _colorBuffer);
        if (scale) effectGraph.SetGraphicsBuffer("ScaleBuffer", _scaleBuffer);
        if (duration) effectGraph.SetGraphicsBuffer("DurationBuffer", _durationBuffer);
    
        effectGraph.SendEvent("Activate");
        effectGraph.SendEvent("Stop");
    
        positions.Dispose();
        angles.Dispose();
        if (color) colors.Dispose();
        if (scale) scales.Dispose();
        if (duration) durations.Dispose();
    }
}

public struct OneShotData
{
    public FixedString64Bytes Name;
    public float3 Position;
    public float3 Angle;
    public float3 Color;
    public float3 Scale;
    public float Duration;
    public float Buffer1;
    public float Buffer2;
    public float Buffer3;
}
