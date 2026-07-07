using System;
using System.Collections;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public sealed class InspectorPropertyMetadata
    {
        public readonly string propertyName;
        public readonly FieldInfo fieldInfo;
        public readonly GUIContent label;
        public readonly LoogaBoxAttribute boxAttribute;
        public readonly LoogaFoldoutAttribute foldoutAttribute;
        public readonly LoogaToggleFoldoutAttribute toggleFoldoutAttribute;
        public readonly InlineRowAttribute inlineRowAttribute;
        public readonly bool hasCustomDrawer;

        private InspectorPropertyMetadata(
            FieldInfo fieldInfo,
            GUIContent label,
            LoogaBoxAttribute boxAttribute,
            LoogaFoldoutAttribute foldoutAttribute,
            LoogaToggleFoldoutAttribute toggleFoldoutAttribute,
            InlineRowAttribute inlineRowAttribute,
            bool hasCustomDrawer)
        {
            this.fieldInfo = fieldInfo;
            propertyName = fieldInfo.Name;
            this.label = label;
            this.boxAttribute = boxAttribute;
            this.foldoutAttribute = foldoutAttribute;
            this.toggleFoldoutAttribute = toggleFoldoutAttribute;
            this.inlineRowAttribute = inlineRowAttribute;
            this.hasCustomDrawer = hasCustomDrawer;
        }

        public static InspectorPropertyMetadata Create(FieldInfo fieldInfo)
        {
            LabelAttribute labelAttribute = fieldInfo.GetCustomAttribute<LabelAttribute>();
            string labelText = labelAttribute == null
                ? ObjectNames.NicifyVariableName(fieldInfo.Name)
                : labelAttribute.label;

            return new InspectorPropertyMetadata(
                fieldInfo,
                PropertyUtils.GetContent(labelText),
                fieldInfo.GetCustomAttribute<LoogaBoxAttribute>(),
                fieldInfo.GetCustomAttribute<LoogaFoldoutAttribute>(),
                fieldInfo.GetCustomAttribute<LoogaToggleFoldoutAttribute>(),
                fieldInfo.GetCustomAttribute<InlineRowAttribute>(),
                CustomDrawerUtil.HasCustomDrawer(GetDrawableType(fieldInfo.FieldType)));
        }

        private static Type GetDrawableType(Type fieldType)
        {
            if (fieldType == null)
                return null;

            if (fieldType.IsArray)
                return fieldType.GetElementType();

            if (typeof(IList).IsAssignableFrom(fieldType) && fieldType.IsGenericType)
                return fieldType.GetGenericArguments()[0];

            return fieldType;
        }
    }
}
