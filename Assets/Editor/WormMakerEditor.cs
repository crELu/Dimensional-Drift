using Enemies.AI;
using Enemies.Worm;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(WormAI))]
    public class PrefabInstantiatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
        
            WormAI script = (WormAI)target;
        
            if (GUILayout.Button("Instantiate Prefabs"))
            {
                InstantiatePrefabs(script);
            }
        }
    
        private void InstantiatePrefabs(WormAI script)
        {
            if (script.bodyPrefab == null)
            {
                Debug.LogError("No prefab assigned!");
                return;
            }
        
            // Clear existing children
            for (int i = script.bodyContainer.childCount - 1; i >= 0; i--)
            {
                var t = script.bodyContainer.GetChild(i).gameObject;
                if (t.TryGetComponent(out WormBodyAuthor _)) DestroyImmediate(t);
            }

            GameObject head = script.gameObject;
            GameObject previous = head;
        
            for (int i = 0; i < script.count; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(script.bodyPrefab, script.bodyContainer);
                var g = instance.GetComponent<WormBodyAuthor>();
                g.head = head;
                g.prev = previous;
            
                previous = instance;
            }
        }
    }
}