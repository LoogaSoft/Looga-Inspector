using System;
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
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            var scenesList = EditorBuildSettings.scenes
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
                .ToList();

            scenesList.Insert(0, "None");
            
            var scenesArray = scenesList
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .ToArray();
            
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.Popup(position, label.text, property.intValue, scenesArray);
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                var currentIndex = Mathf.Max(0, Array.IndexOf(scenesArray, property.stringValue));
                var newIndex = EditorGUI.Popup(position, label.text, currentIndex, scenesArray);
                property.stringValue = scenesArray[newIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use SceneAttribute with ints or strings only");
            }
            
            EditorGUI.EndProperty();
        }
    }
}