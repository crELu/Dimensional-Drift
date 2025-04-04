using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(SpritePreviewAttribute))]
    public class SpritePreviewDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
        
            // Draw the property field normally
            position.height = EditorGUIUtility.singleLineHeight;
            //EditorGUI.PropertyField(position, property, label);
            
            Sprite sprite = property.objectReferenceValue as Sprite;

            // display loaded sprite
            sprite = EditorGUILayout.ObjectField(label, sprite, typeof(Sprite), false) as Sprite;

            // update sprite to selection
            property.objectReferenceValue = sprite;
        
            EditorGUI.EndProperty();
        }
    }
}