using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace ECS.Enemy
{
    public class VFXManager: MonoBehaviour
    {
        public static VFXManager main;
        public VisualEffect effect;
        public static Texture2D tex1, tex2;
        private int _currentPointer;
        private int _maxCapacity;
        private int _capacity;
        private Dictionary<string, VFXType> _registeredVFX = new ();
        
        private void Awake()
        {
            tex1 = TrailGraph.CreateTex(512);
            tex2 = TrailGraph.CreateTex(512);
            main = this;
            effect.SetTexture("Positions", tex1);
            effect.SetTexture("ColorLife", tex2);
        }
        
        public (int, int, Texture2D, Texture2D) RegisterTrail()
        {
            var x = _currentPointer / tex1.width;
            var y = _currentPointer % tex1.width;
            _capacity++;
            _currentPointer++;
            return (x, y, tex1, tex2);
        }
        
        public (VFXData, int)? RegisterParticle(string vfxName)
        {
            if (!_registeredVFX.ContainsKey(vfxName))
            {
                Debug.Log($"There is no registered VFX with the name {vfxName}.");
                return null;
            }
            
            return _registeredVFX[vfxName].RegisterParticle();
        }

        public void UnregisterParticle(string vfxName, int graphId)
        {
            if (!_registeredVFX.ContainsKey(vfxName))
            {
                Debug.Log($"There is no registered VFX with the name {vfxName}.");
                return;
            }
            _registeredVFX[vfxName].UnregisterParticle(graphId);
        }
    }
}