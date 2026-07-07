using System;
using System.Collections;
using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(FilteredEnumAttribute))]
    public sealed class FilteredEnumDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            Type enumType = fieldInfo?.FieldType;
            if (property.propertyType != SerializedPropertyType.Enum || enumType == null || !enumType.IsEnum)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            FilteredEnumAttribute filteredEnum = (FilteredEnumAttribute)attribute;
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            List<int> values = GetFilteredValues(target, enumType, filteredEnum.ProviderMemberName);
            if (values.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            string[] names = Enum.GetNames(enumType);
            Array rawValues = Enum.GetValues(enumType);
            List<GUIContent> labels = new(values.Count);
            int currentValue = Convert.ToInt32(rawValues.GetValue(property.enumValueIndex));
            int selectedIndex = 0;

            for (int i = 0; i < values.Count; i++)
            {
                int value = values[i];
                string name = Enum.GetName(enumType, value) ?? value.ToString();
                labels.Add(PropertyUtils.GetContent(ObjectNames.NicifyVariableName(name)));

                if (value == currentValue)
                    selectedIndex = i;
            }

            int newIndex = EditorGUI.Popup(position, label, selectedIndex, labels.ToArray());
            int newValue = values[Mathf.Clamp(newIndex, 0, values.Count - 1)];

            for (int i = 0; i < rawValues.Length; i++)
            {
                if (Convert.ToInt32(rawValues.GetValue(i)) == newValue)
                {
                    property.enumValueIndex = i;
                    return;
                }
            }
        }

        private static List<int> GetFilteredValues(object target, Type enumType, string providerMemberName)
        {
            List<int> values = new();
            object raw = LoogaMemberValueUtility.GetValue(target, providerMemberName);
            if (raw is string stringValue)
            {
                TryAddValue(values, enumType, stringValue);
                return values;
            }

            if (raw is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                    TryAddValue(values, enumType, item);
                return values;
            }

            TryAddValue(values, enumType, raw);
            return values;
        }

        private static void TryAddValue(List<int> values, Type enumType, object value)
        {
            if (value == null)
                return;

            int intValue;
            if (value.GetType().IsEnum)
                intValue = Convert.ToInt32(value);
            else if (value is int rawInt)
                intValue = rawInt;
            else if (value is string rawString && Enum.TryParse(enumType, rawString, true, out object parsed))
                intValue = Convert.ToInt32(parsed);
            else
                return;

            if (!values.Contains(intValue))
                values.Add(intValue);
        }
    }
}
