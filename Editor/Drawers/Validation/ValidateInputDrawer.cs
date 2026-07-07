using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(ValidateInputAttribute))]
    public class ValidateInputDrawer : PropertyDrawer
    {
        private const float BOX_HEIGHT = 30f;
        private readonly float _spacing = EditorGUIUtility.standardVerticalSpacing;
        
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsPropertyValid(property))
                return EditorGUI.GetPropertyHeight(property, label) + BOX_HEIGHT + _spacing;
            
            return EditorGUI.GetPropertyHeight(property, label);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ValidateInputAttribute attr = attribute as ValidateInputAttribute;
            
            if (IsPropertyValid(property))
            {
                Rect boxRect = new Rect(position.x, position.y, position.width, BOX_HEIGHT);
                EditorGUI.HelpBox(boxRect, attr.message, GetMessageType(attr.messageMode));
                
                Rect propertyRect = new Rect(position.x, position.y + BOX_HEIGHT + _spacing, position.width, position.height - BOX_HEIGHT + _spacing);
                EditorGUI.PropertyField(propertyRect, property, label, true);
            }
            else
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        private bool IsPropertyValid(SerializedProperty property)
        {
            ValidateInputAttribute attr = attribute as ValidateInputAttribute;
            return GetCondition(property.serializedObject.targetObject, attr.condition);
        }

        public static bool GetCondition(object target, string boolName)
        {
            if (target == null) 
                return false;
            
            var type = target.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            
            FieldInfo field = type.GetField(boolName, flags);
            if (field != null && field.FieldType == typeof(bool))
                return (bool)field.GetValue(target);
            
            PropertyInfo property = type.GetProperty(boolName, flags);
            if (property != null && property.PropertyType == typeof(bool))
                return (bool)property.GetValue(target);
            
            MethodInfo method = type.GetMethod(boolName, flags);
            if (method != null && method.ReturnType == typeof(bool))
                return (bool)method.Invoke(target, null);
            
            return false;
        }

        public static MessageType GetMessageType(MessageMode mode)
        {
            return mode switch
            {
                MessageMode.Error => MessageType.Error,
                MessageMode.Warning => MessageType.Warning,
                MessageMode.Info => MessageType.Info,
                _ => MessageType.None
            };
        }
    }
}