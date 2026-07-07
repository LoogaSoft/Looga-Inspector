using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(AssetDisplayNameAttribute))]
    public sealed class AssetDisplayNameDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            AssetDisplayNameAttribute displayNameAttribute = (AssetDisplayNameAttribute)attribute;
            bool useCustomName = GetUseCustomName(property, displayNameAttribute.useCustomNameMember);

            if (!useCustomName)
                property.stringValue = GetDefaultDisplayName(property);

            using (new EditorGUI.DisabledScope(!useCustomName))
                EditorGUI.PropertyField(position, property, label, true);
        }

        private static bool GetUseCustomName(SerializedProperty property, string memberName)
        {
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            if (target == null)
                return false;

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            FieldInfo field = target.GetType().GetField(memberName, flags);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);

            PropertyInfo propertyInfo = target.GetType().GetProperty(memberName, flags);
            if (propertyInfo != null && propertyInfo.PropertyType == typeof(bool))
                return (bool)propertyInfo.GetValue(target);

            return false;
        }

        private static string GetDefaultDisplayName(SerializedProperty property)
        {
            Object targetObject = property.serializedObject.targetObject;
            if (targetObject == null)
                return string.Empty;

            return ObjectNames.NicifyVariableName(targetObject.name);
        }
    }
}
