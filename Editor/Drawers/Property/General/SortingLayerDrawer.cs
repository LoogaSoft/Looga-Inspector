using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

#if ZLINQ_SUPPORT
using ZLinq;
#else
using System.Linq;
#endif

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            var sortingLayers = SortingLayer.layers
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(s => s.name)
                .ToList();
            
            sortingLayers.Insert(0, "None");
            
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, sortingLayers.ToArray());
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                var currentIndex = Mathf.Max(0, sortingLayers.IndexOf(property.stringValue));
                var newIndex = EditorGUI.Popup(position, label.text, currentIndex, sortingLayers.ToArray());
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