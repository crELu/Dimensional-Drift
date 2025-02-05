using System;
using UnityEngine;
using UnityEngine.VFX;

namespace ECS.Enemy
{
    public class TrailManager: MonoBehaviour
    {
        public static TrailManager main;
        public VisualEffect effect;
        public static Texture2D tex1, tex2;
        private int _currentPointer;
        private int _maxCapacity;
        private int _capacity;
        
        private void Awake()
        {
            tex1 = CreateTex(512);
            tex2 = CreateTex(512);
            main = this;
            effect.SetTexture("Positions", tex1);
            effect.SetTexture("ColorLife", tex2);
        }

        private Texture2D CreateTex(int dim)
        {
            var tex = new Texture2D(dim, dim, TextureFormat.RGBAFloat, false);
            for (int i = 0; i < dim; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    tex.SetPixel(i, j, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        public (int, int, Texture2D, Texture2D) RegisterTrail()
        {
            var x = _currentPointer / tex1.width;
            var y = _currentPointer % tex1.width;
            _capacity++;
            _currentPointer++;
            return (x, y, tex1, tex2);
        }
    }
}