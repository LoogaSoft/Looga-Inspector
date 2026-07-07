using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            List<string> sortingLayers = LoogaInspectorQueryUtility.GetSortingLayerNames(SortingLayer.layers);
            sortingLayers.Insert(0, "None");
            string[] sortingLayerArray = sortingLayers.ToArray();
            
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, sortingLayerArray);
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                var currentIndex = Mathf.Max(0, sortingLayers.IndexOf(property.stringValue));
                var newIndex = EditorGUI.Popup(position, label.text, currentIndex, sortingLayerArray);
                property.stringValue = sortingLayers[newIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use SortingLayerAttribute with ints or strings only");
            }
            
            EditorGUI.EndProperty();
        }
    }
}
