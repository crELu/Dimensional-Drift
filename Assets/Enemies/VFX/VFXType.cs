using System;
using UnityEngine;
using UnityEngine.VFX;
using Object = UnityEngine.Object;

namespace ECS.Enemy
{
    public class VFXType: MonoBehaviour
    {
        public new string name;
        public int capacity;
        public int texSize;
        public VisualEffect effectPrefab;
        
        private TrailGraph[] _graphs;
        private int _workingGraph;
        private void Start()
        {
            _graphs = new TrailGraph[capacity];
        }
        
        public (VFXData, int)? RegisterParticle()
        {
            if (_graphs[_workingGraph].Filled)
            {
                _workingGraph = Array.IndexOf(_graphs, null);
                if (_workingGraph == -1)
                {
                    Debug.Log($"Ran out of space on the current VFX: {name}.");
                    return null;
                }

                _graphs[_workingGraph] = new TrailGraph(texSize, effectPrefab);
            }

            var data = _graphs[_workingGraph].RegisterTrail();
            return (data, _workingGraph);
        }

        public void UnregisterParticle(int graphId)
        {
            var graph = _graphs[graphId];
            if (graph == null)
            {
                Debug.Log($"Tried to remove from null VFX: {name}.");
                return;
            }
            graph.Free();
            if (graph.Complete) _graphs[graphId] = null;
        }
    }
    
    public class TrailGraph
    {
        public Texture2D Positions, ColorLife, Size;
        public VisualEffect Effect;
        private int _capacity, _active;
        private int _currentPointer;
        private int _w;
        
        public TrailGraph(int s, VisualEffect effectPrefab)
        {
            Effect = Object.Instantiate(effectPrefab);
            Positions = CreateTex(s);
            ColorLife = CreateTex(s);
            Size = CreateTex(s);
            _capacity = s * s;
            Effect.SetTexture("Positions", Positions);
            Effect.SetTexture("ColorLife", ColorLife);
            Effect.SetTexture("Size", Size);
        }
        
        public bool Filled => _currentPointer >= _capacity;
        public bool Complete => Filled && _active == 0;
        public void Free()
        {
            _active--;
        }
        public VFXData RegisterTrail()
        {
            var x = _currentPointer / _w;
            var y = _currentPointer % _w;
            _active++;
            _currentPointer++;
            return new VFXData{X=x, Y=y, Positions = Positions, ColorLife = ColorLife, Size = Size};
        }
        
        public static Texture2D CreateTex(int dim)
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
    }
    
    public struct VFXData
    {
        public int X, Y;
        public Texture2D Positions, ColorLife, Size;
    }
}