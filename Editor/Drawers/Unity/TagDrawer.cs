using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            List<string> tagsList = LoogaInspectorQueryUtility.ToStringList(InternalEditorUtility.tags);
            tagsList.Insert(0, "None");
            string[] tagsArray = tagsList.ToArray();
            
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.String)
            {
                var currentIndex = Mathf.Max(0, tagsList.IndexOf(property.stringValue));
                var newIndex = EditorGUI.Popup(position, label.text, currentIndex, tagsArray);
                property.stringValue = tagsList[newIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use TagAttribute with strings only");
            }
            
            EditorGUI.EndProperty();
        }
    }
}
