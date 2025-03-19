using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.VFX;

namespace ECS.Enemy
{
    public class VFXManager: MonoBehaviour
    {
        public static VFXManager main;
        
        private Dictionary<FixedString64Bytes, VFXType> _registeredPersistentVFX = new ();
        private Dictionary<FixedString64Bytes, VFXType> _registeredOneShotVFX = new ();
        
        private void Awake()
        {
            main = this;
            foreach (var vfx in  GetComponentsInChildren<VFXType>())
            {
                _registeredPersistentVFX[vfx.name] = vfx;
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
}