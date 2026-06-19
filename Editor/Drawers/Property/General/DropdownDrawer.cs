using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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
    [CustomPropertyDrawer(typeof(DropdownAttribute))]
    public class DropdownDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            DropdownAttribute dropdownAttribute = (DropdownAttribute)attribute;
            object target = property.serializedObject.targetObject;

            List<object> options = GetOptions(target, dropdownAttribute.listOrArrayName);

            if (options == null || options.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }
            
            string[] labels = options
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(o => GetOptionLabel(o, dropdownAttribute))
                .ToArray();
            
            EditorGUI.BeginProperty(position, label, property);

            object currentObj = property.boxedValue;
            int currentIndex = 0;

            for (int i = 0; i < options.Count; i++)
            {
                if (Equals(currentObj, GetOptionValue(options[i], dropdownAttribute)))
                {
                    currentIndex = i;
                    break;
                }
            }
            
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, labels);
            
            if (newIndex != currentIndex)
                property.boxedValue = GetOptionValue(options[newIndex], dropdownAttribute);
            
            EditorGUI.EndProperty();
        }

        private List<object> GetOptions(object target, string propertyName)
        {
            System.Type type = target.GetType();
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

            object result = null;
            FieldInfo field = type.GetField(propertyName, flags);
            
            if (field != null)
                result = field.GetValue(target);
            else
            {
                PropertyInfo property = type.GetProperty(propertyName, flags);
                
                if (property != null)
                    result = property.GetValue(target, null);
                else
                {
                    MethodInfo method = type.GetMethod(propertyName, flags);
                    
                    if (method != null)
                        result = method.Invoke(target, null);
                    else 
                        return null;
                }
            }

            if (result is IEnumerable enumerable)
            {
                #if ZLINQ_SUPPORT
                return enumerable.AsValueEnumerable<object>().ToList();
                #else
                return enumerable.Cast<object>().ToList();
                #endif           
            }

            return null;
        }

        private static string GetOptionLabel(object option, DropdownAttribute dropdownAttribute)
        {
            if (option == null)
                return "Null";

            if (option is DropdownOption dropdownOption)
                return dropdownOption.Label ?? "Null";

            if (!string.IsNullOrWhiteSpace(dropdownAttribute.labelMember)
                && TryGetMemberValue(option, dropdownAttribute.labelMember, out object labelValue))
                return labelValue?.ToString() ?? "Null";

            return option.ToString() ?? "Null";
        }

        private static object GetOptionValue(object option, DropdownAttribute dropdownAttribute)
        {
            if (option is DropdownOption dropdownOption)
                return dropdownOption.Value;

            if (!string.IsNullOrWhiteSpace(dropdownAttribute.valueMember)
                && TryGetMemberValue(option, dropdownAttribute.valueMember, out object value))
                return value;

            return option;
        }

        private static bool TryGetMemberValue(object source, string memberName, out object value)
        {
            value = null;
            if (source == null || string.IsNullOrWhiteSpace(memberName))
                return false;

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            System.Type type = source.GetType();

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
            {
                value = field.GetValue(source);
                return true;
            }

            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null)
            {
                value = property.GetValue(source, null);
                return true;
            }

            MethodInfo method = type.GetMethod(memberName, flags);
            if (method != null && method.GetParameters().Length == 0)
            {
                value = method.Invoke(source, null);
                return true;
            }

            return false;
        }
    }
}
