using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class DecoratorSystem
    {
        private static readonly Dictionary<Type, Type> _drawerCache = new();
        private static readonly FieldInfo _attributeField;

        static DecoratorSystem()
        {
            _attributeField = typeof(DecoratorDrawer).GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
            
            var drawerTypes = TypeCache.GetTypesDerivedFrom<DecoratorDrawer>();

            foreach (var drawerType in drawerTypes)
            {
                var customDrawerAttr = drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), true)
                    .Cast<CustomPropertyDrawer>()
                    .FirstOrDefault();

                if (customDrawerAttr != null)
                {
                    var typeField = typeof(CustomPropertyDrawer).GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    if (typeField != null)
                    {
                        var targetAttrType = (Type)typeField.GetValue(customDrawerAttr);
                        if (targetAttrType != null && !_drawerCache.ContainsKey(targetAttrType))
                            _drawerCache[targetAttrType] = drawerType;
                    }
                }
            }
        }
        public static void DrawDecorators(SerializedProperty property, object targetObject)
        {
            bool isList = property.isArray && property.propertyType != SerializedPropertyType.String;
            if (!isList) 
                return;
            
            FieldInfo fieldInfo = targetObject.GetType().GetField(property.name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (fieldInfo == null) 
                return;

            var attributes = fieldInfo.GetCustomAttributes<PropertyAttribute>();
            foreach (var attr in attributes)
            {
                if (_drawerCache.TryGetValue(attr.GetType(), out Type drawerType))
                    DrawDecoratorInstance(drawerType, attr);
            }
        }

        private static void DrawDecoratorInstance(Type drawerType, PropertyAttribute attr)
        {
            var drawerInstance = (DecoratorDrawer)Activator.CreateInstance(drawerType);
            
            if (_attributeField != null)
                _attributeField.SetValue(drawerInstance, attr);

            float height = drawerInstance.GetHeight();
            Rect position = EditorGUILayout.GetControlRect(false, height);
            
            drawerInstance.OnGUI(position);
        }
    }
}