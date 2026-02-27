using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace LoogaSoft.Inspector.Editor
{
    public static class CustomDrawerUtil
    {
        private static readonly Dictionary<Type, bool> DrawerCache = new();

        private static readonly List<Type> InheritableTypes = new();
        private static readonly HashSet<Type> ExactDrawerTypes = new();

        static CustomDrawerUtil()
        {
            BuildCache();
        }
        private static void BuildCache()
        {
            ExactDrawerTypes.Clear();
            InheritableTypes.Clear();
            DrawerCache.Clear();
            
            var drawerTypes = TypeCache.GetTypesDerivedFrom<PropertyDrawer>();

            foreach (var drawerType in drawerTypes)
            {
                var attributes = Attribute.GetCustomAttributes(drawerType, typeof(CustomPropertyDrawer));

                foreach (var attribute in attributes)
                {
                    if (attribute is CustomPropertyDrawer attr)
                    {
                        FieldInfo typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);
                        FieldInfo childField = typeof(CustomPropertyDrawer).GetField("m_UseForChildren", BindingFlags.Instance | BindingFlags.NonPublic);

                        if (typeField != null)
                        {
                            Type targetType = (Type)typeField.GetValue(attr);
                            bool useForChildren = childField != null && (bool)childField.GetValue(attr);
                            
                            if (useForChildren)
                                InheritableTypes.Add(targetType);
                            else
                                ExactDrawerTypes.Add(targetType);
                        }
                    }
                }
            }
        }

        public static bool HasCustomDrawer(SerializedProperty property)
        {
            Type targetType = GetTargetType(property);
            if (targetType == null)
                return false;
            
            if (DrawerCache.TryGetValue(targetType, out bool result))
                return result;
            
            bool hasDrawer = CheckType(targetType);
            
            DrawerCache[targetType] = hasDrawer;
            
            return hasDrawer;
        }

        private static bool CheckType(Type type)
        {
            if (ExactDrawerTypes.Contains(type))
                return true;
            
            foreach (Type inheritableType in InheritableTypes)
            {
                if (inheritableType.IsAssignableFrom(type))
                    return true;
            }
            
            return false;
        }
        public static Type GetTargetType(SerializedProperty property)
        {
            if (property == null) 
                return null;
            
            Type parentType = property.serializedObject.targetObject.GetType();
            
            string[] pathParts = property.propertyPath.Split('.');

            Type currentType = parentType;

            foreach (string part in pathParts)
            {
                if (part == "Array" || part.StartsWith("data["))
                    continue;
                
                FieldInfo field = GetField(currentType, part);
                if (field == null)
                    return null;
                
                currentType = field.FieldType;

                if (typeof(System.Collections.IList).IsAssignableFrom(currentType))
                {
                    if (currentType.IsArray)
                        currentType = currentType.GetElementType();
                    else if (currentType.IsGenericType)
                        currentType = currentType.GetGenericArguments()[0];
                    else
                        return null;
                }
            }
            
            return currentType;
        }
        private static FieldInfo GetField(Type type, string fieldName)
        {
            while (type != null)
            {
                var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null) 
                    return field;
                type = type.BaseType;
            }
            return null;
        }
    }
}













