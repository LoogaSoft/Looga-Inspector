using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(NoDuplicateEntriesAttribute))]
    public sealed class NoDuplicateEntriesDrawer : PropertyDrawerBase
    {
        private const float WarningSpacing = 2f;

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            NoDuplicateEntriesAttribute duplicateAttribute = (NoDuplicateEntriesAttribute)attribute;
            bool hasDuplicates = HasDuplicates(property, duplicateAttribute.memberName);
            float warningHeight = hasDuplicates
                ? EditorGUIUtility.singleLineHeight * 2f
                : 0f;

            Rect propertyRect = new(
                position.x,
                position.y,
                position.width,
                position.height - warningHeight - (hasDuplicates ? WarningSpacing : 0f));

            EditorGUI.PropertyField(propertyRect, property, label, true);

            if (!hasDuplicates)
                return;

            Rect warningRect = new(
                position.x,
                propertyRect.yMax + WarningSpacing,
                position.width,
                warningHeight);

            EditorGUI.HelpBox(warningRect, duplicateAttribute.message, MessageType.Warning);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            NoDuplicateEntriesAttribute duplicateAttribute = (NoDuplicateEntriesAttribute)attribute;
            float height = EditorGUI.GetPropertyHeight(property, label, true);

            if (HasDuplicates(property, duplicateAttribute.memberName))
                height += WarningSpacing + EditorGUIUtility.singleLineHeight * 2f;

            return height;
        }

        private static bool HasDuplicates(SerializedProperty property, string memberName)
        {
            if (!property.isArray)
                return false;

            HashSet<string> seenValues = new();

            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                string key = GetComparableValue(element, memberName);

                if (string.IsNullOrEmpty(key))
                    continue;

                if (!seenValues.Add(key))
                    return true;
            }

            return false;
        }

        private static string GetComparableValue(SerializedProperty property, string memberName)
        {
            SerializedProperty valueProperty = string.IsNullOrWhiteSpace(memberName)
                ? property
                : property.FindPropertyRelative(memberName);

            if (valueProperty == null)
                return null;

            return valueProperty.propertyType switch
            {
                SerializedPropertyType.ObjectReference => valueProperty.objectReferenceValue != null
                    ? valueProperty.objectReferenceValue.GetInstanceID().ToString()
                    : null,
                SerializedPropertyType.String => valueProperty.stringValue,
                SerializedPropertyType.Integer => valueProperty.intValue.ToString(),
                SerializedPropertyType.Boolean => valueProperty.boolValue.ToString(),
                SerializedPropertyType.Enum => valueProperty.enumValueIndex.ToString(),
                SerializedPropertyType.Float => valueProperty.floatValue.ToString("R"),
                _ => valueProperty.propertyPath
            };
        }
    }
}
