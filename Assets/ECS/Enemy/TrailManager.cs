using System;
using UnityEngine;
using UnityEngine.VFX;

namespace ECS.Enemy
{
    public class TrailManager: MonoBehaviour
    {
        public static TrailManager main;
        public VisualEffect effect;
        public static Texture2D tex;
        private int _currentPointer;
        private int _maxCapacity;
        private int _capacity;
        
        private void Awake()
        {
            tex = new Texture2D(512, 512, TextureFormat.RGBAFloat, false);
            for (int y = 0; y < tex.height; y++)
            {
                for (int x = 0; x < tex.width; x++)
                {
                    Color color = Color.clear;
                    tex.SetPixel(x, y, color);
                }
            }
            tex.Apply();
            main = this;
            effect.SetTexture("Positions", tex);
        }

        public (int, int, Texture2D) RegisterTrail()
        {
            var x = _currentPointer / tex.width;
            var y = _currentPointer % tex.width;
            _capacity++;
            _currentPointer++;
            return (x, y, tex);
        }
    }
}