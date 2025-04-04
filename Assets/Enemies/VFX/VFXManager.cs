using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;


public class VFXManager: MonoBehaviour
{
    public static VFXManager main;
    
    private Dictionary<FixedString64Bytes, VFXType> _registeredPersistentVFX = new ();
    private Dictionary<FixedString64Bytes, VFXOneShot> _registeredOneShotVFX = new ();
    
    private void Awake()
    {
        main = this;
        foreach (var vfx in  GetComponentsInChildren<VFXType>())
        {
            _registeredPersistentVFX[vfx.name] = vfx;
        }
        foreach (var vfx in  GetComponentsInChildren<VFXOneShot>())
        {
            _registeredOneShotVFX[vfx.name] = vfx;
        }
    }
    
    public void PlayOneShot(NativeArray<OneShotData> data)
    {
        Dictionary<FixedString64Bytes, List<OneShotData>> groupedEffects = new();
    
        foreach (var entry in data)
        {
            if (_registeredOneShotVFX.TryGetValue(entry.Name, out var vfxType))
            {
                if (!groupedEffects.ContainsKey(vfxType.name))
                    groupedEffects[vfxType.name] = new List<OneShotData>();
            
                groupedEffects[vfxType.name].Add(entry);
            }
            else
            {
                Debug.Log($"There is no OneShotVFX with the name {entry.Name}");
            }
        }
    
        foreach (var kvp in _registeredOneShotVFX)
        {
            if (groupedEffects.TryGetValue(kvp.Key, out var effect)) _registeredOneShotVFX[kvp.Key].PlayEffects(effect);
            else _registeredOneShotVFX[kvp.Key].PlayEffects(new());
        }
    }

    
    public (VFXData, int)? RegisterParticle(FixedString64Bytes vfxName)
    {
        if (!_registeredPersistentVFX.ContainsKey(vfxName))
        {
            Debug.Log($"There is no registered VFX with the name {vfxName}.");
            return null;
        }
        
        return _registeredPersistentVFX[vfxName].RegisterParticle();
    }

    public void UnregisterParticles(FixedString64Bytes vfxName, int graphId, int count)
    {
        if (!_registeredPersistentVFX.ContainsKey(vfxName))
        {
            Debug.Log($"There is no registered VFX with the name {vfxName}.");
            return;
        }
        _registeredPersistentVFX[vfxName].UnregisterParticle(graphId, count);
    }
}

