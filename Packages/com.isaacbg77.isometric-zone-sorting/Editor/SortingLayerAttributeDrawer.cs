using UnityEditor;
using UnityEngine;

namespace IsometricZoneSorting.Editor
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "[SortingLayer] must be used on a string field");
                return;
            }

            var layers = SortingLayer.layers;
            var names = new string[layers.Length];
            var selectedIndex = 0;
            for (var i = 0; i < layers.Length; i++)
            {
                names[i] = layers[i].name;
                if (names[i] == property.stringValue) selectedIndex = i;
            }

            EditorGUI.BeginProperty(position, label, property);
            var newIndex = EditorGUI.Popup(position, label.text, selectedIndex, names);
            if (newIndex >= 0 && newIndex < names.Length)
            {
                property.stringValue = names[newIndex];
            }
            EditorGUI.EndProperty();
        }
    }
}
