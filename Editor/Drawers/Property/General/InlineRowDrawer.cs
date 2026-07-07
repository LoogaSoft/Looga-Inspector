using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(InlineRowAttribute))]
    public sealed class InlineRowDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Generic || !property.hasVisibleChildren || property.isArray)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Rect contentRect = EditorGUI.PrefixLabel(position, label);
            List<SerializedProperty> children = InlineRowEditorUtility.GetVisibleChildren(property);
            List<GUIContent> labels = new(children.Count);

            for (int i = 0; i < children.Count; i++)
                labels.Add(PropertyUtils.GetContent(children[i].displayName));

            InlineRowEditorUtility.DrawProperties(contentRect, children, labels);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            return InlineRowEditorUtility.SingleLineHeight;
        }
    }
}
