using System;
using System.Collections.Generic;
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

            FieldInfo typeField = typeof(CustomPropertyDrawer).GetField(
                "m_Type",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

            if (typeField == null)
                return;

            var drawerTypes = TypeCache.GetTypesDerivedFrom<DecoratorDrawer>();
            foreach (Type drawerType in drawerTypes)
            {
                object[] drawerAttributes = drawerType.GetCustomAttributes(typeof(CustomPropertyDrawer), true);
                for (int i = 0; i < drawerAttributes.Length; i++)
                {
                    if (drawerAttributes[i] is not CustomPropertyDrawer customDrawerAttribute)
                        continue;

                    Type targetAttributeType = (Type)typeField.GetValue(customDrawerAttribute);
                    if (targetAttributeType != null && !_drawerCache.ContainsKey(targetAttributeType))
                        _drawerCache[targetAttributeType] = drawerType;

                    break;
                }
            }
        }

        public static void DrawDecorators(SerializedProperty property, object targetObject)
        {
            bool isList = property.isArray && property.propertyType != SerializedPropertyType.String;
            if (!isList) 
                return;
            
            PropertyAttribute[] attributes = PropertyUtils.GetAttributes<PropertyAttribute>(property);
            for (int i = 0; i < attributes.Length; i++)
            {
                PropertyAttribute attribute = attributes[i];
                if (_drawerCache.TryGetValue(attribute.GetType(), out Type drawerType))
                    DrawDecoratorInstance(drawerType, attribute);
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