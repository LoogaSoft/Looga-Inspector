using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class PropertyUtils
    {
        public static T GetAttribute<T>(SerializedProperty property) where T : class
        {
            if (property == null) 
                return null;
            
            T[] attributes = GetAttributes<T>(property);
            return attributes.Length > 0 ? attributes[0] : null;
        }
        
        public static T[] GetAttributes<T>(SerializedProperty property) where T : class
        {
            if (property == null) 
                return new T[] { };
            
            FieldInfo field = ReflectionUtils.GetField(GetTargetObjectWithProperty(property), property.name);
            if (field == null)
                return new T[] { };
            
            return field.GetCustomAttributes(typeof(T), true) as T[];
        }

        public static GUIContent GetLabel(SerializedProperty property)
        {
            LabelAttribute labelAttribute = GetAttribute<LabelAttribute>(property);
            string labelString = labelAttribute == null ? property.displayName : labelAttribute.label;
            return new GUIContent(labelString);
        }

        public static void CallOnFieldChangedCallbacks(SerializedProperty property)
        {
            OnFieldChangedAttribute[] onFieldChangedAttributes = GetAttributes<OnFieldChangedAttribute>(property);
            if (onFieldChangedAttributes.Length == 0) 
                return;

            object target = GetTargetObjectWithProperty(property);
            property.serializedObject.ApplyModifiedProperties();

            foreach (var onFieldChangedAttribute in onFieldChangedAttributes)
            {
                MethodInfo method = ReflectionUtils.GetMethod(target, onFieldChangedAttribute.MethodName);
                if (method != null && method.ReturnType == typeof(void) && method.GetParameters().Length == 0) 
                    method.Invoke(target, null);
                else 
                    Debug.LogWarning(onFieldChangedAttribute.GetType().Name
                                     + "callback is invalid, callback must be void with no parameters"
                                     , property.serializedObject.targetObject); 
            }
        }
        
        public static bool IsEnabled(SerializedProperty property)
        {
            if (property == null) 
                return true;
            
            ReadOnlyAttribute readOnlyAttribute = GetAttribute<ReadOnlyAttribute>(property);
            if (readOnlyAttribute != null)
                return false;
            
            EnableIfAttributeBase enableIfAttribute = GetAttribute<EnableIfAttributeBase>(property);
            if (enableIfAttribute == null)
                return true;
            
            object target = GetTargetObjectWithProperty(property);
            
            bool inverted = enableIfAttribute.inverted;
            bool condition = GetCondition(target, enableIfAttribute.condition);

            return inverted ? !condition : condition;
        }

        public static bool IsVisible(SerializedProperty property)
        {
            if (property == null) 
                return true;
            
            ShowIfAttributeBase showIfAttribute = GetAttribute<ShowIfAttributeBase>(property);
            if (showIfAttribute == null)
                return true;
            
            object target = GetTargetObjectWithProperty(property);

            bool inverted = showIfAttribute.inverted;
            bool condition = GetCondition(target, showIfAttribute.condition);
            
            return inverted ? !condition : condition;
        }

        private static bool GetCondition(object target, string conditionName)
        {
            FieldInfo field = ReflectionUtils.GetField(target, conditionName);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);
            
            PropertyInfo property = ReflectionUtils.GetProperty(target, conditionName);
            if (property != null && property.PropertyType == typeof(bool))
                return (bool)property.GetValue(target, null);
            
            MethodInfo method = ReflectionUtils.GetMethod(target, conditionName);
            if (method != null && method.ReturnType == typeof(bool))
                return (bool)method.Invoke(target, null);
            
            Debug.LogWarning($"Could not find condition {conditionName} on {target.GetType().Name}");
            return false;
        }

        public static object GetTargetObjectWithProperty(SerializedProperty property)
        {
            if (property == null) 
                return null;
            
            object obj = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data[", "[");
            string[] elements = path.Split('.');

            for (int i = 0; i < elements.Length - 1; i++)
            {
                string element = elements[i];
                if (element.Contains("["))
                {
                    string elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    int index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
                
                if (obj == null) return null;
            }
            
            return obj;
        }

        private static object GetValue(object source, string name)
        {
            if (source == null) 
                return null;
            
            Type type = source.GetType();
            BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            while (type != null)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null) 
                    return field.GetValue(source);
                
                PropertyInfo property = type.GetProperty(name, flags);
                if (property != null) 
                    return property.GetValue(source, null);
                
                type = type.BaseType;
            }
            
            return null;
        }

        private static object GetValue(object source, string name, int index)
        {
            IEnumerable enumerable = GetValue(source, name) as IEnumerable;
            if (enumerable == null) 
                return null;
            
            IEnumerator enumerator = enumerable.GetEnumerator();
            using var enumerator1 = enumerator as IDisposable;

            for (int i = 0; i <= index; i++) 
            {
                if (!enumerator.MoveNext()) 
                    return null;
            }
            
            return enumerator.Current;
        }
    }
}