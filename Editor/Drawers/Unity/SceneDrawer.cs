using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(SceneAttribute))]
    public class SceneDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            List<string> scenesList = LoogaInspectorQueryUtility.GetSceneNames(EditorBuildSettings.scenes);
            scenesList.Insert(0, "None");
            string[] scenesArray = scenesList.ToArray();
            
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
