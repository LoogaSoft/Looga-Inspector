using System.Collections.Generic;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(ShaderPropertyAttribute))]
    public sealed class ShaderPropertyDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            ShaderPropertyAttribute shaderPropertyAttribute = (ShaderPropertyAttribute)attribute;
            Shader shader = ResolveShader(property, shaderPropertyAttribute.materialOrShaderMember);
            if (shader == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            List<string> propertyNames = GetPropertyNames(shader, shaderPropertyAttribute.propertyType);
            if (propertyNames.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            int currentIndex = Mathf.Max(0, propertyNames.IndexOf(property.stringValue));
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, propertyNames.ToArray());
            property.stringValue = propertyNames[Mathf.Clamp(newIndex, 0, propertyNames.Count - 1)];
        }

        private static Shader ResolveShader(SerializedProperty property, string memberName)
        {
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            object memberValue = GetMemberValue(target, memberName);

            return memberValue switch
            {
                Material material => material != null ? material.shader : null,
                Shader shader => shader,
                _ => null
            };
        }

        private static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            System.Type type = target.GetType();

            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = type.GetProperty(memberName, flags);
            if (property != null)
                return property.GetValue(target, null);

            MethodInfo method = type.GetMethod(memberName, flags);
            if (method != null && method.GetParameters().Length == 0)
                return method.Invoke(target, null);

            return null;
        }

        private static List<string> GetPropertyNames(Shader shader, LoogaShaderPropertyType propertyType)
        {
            List<string> names = new();
            int propertyCount = ShaderUtil.GetPropertyCount(shader);

            for (int i = 0; i < propertyCount; i++)
            {
                if (!MatchesType(shader, i, propertyType))
                    continue;

                names.Add(ShaderUtil.GetPropertyName(shader, i));
            }

            return names;
        }

        private static bool MatchesType(Shader shader, int propertyIndex, LoogaShaderPropertyType propertyType)
        {
            if (propertyType == LoogaShaderPropertyType.Any)
                return true;

            ShaderUtil.ShaderPropertyType shaderPropertyType = ShaderUtil.GetPropertyType(shader, propertyIndex);
            return propertyType switch
            {
                LoogaShaderPropertyType.Color => shaderPropertyType == ShaderUtil.ShaderPropertyType.Color,
                LoogaShaderPropertyType.Vector => shaderPropertyType == ShaderUtil.ShaderPropertyType.Vector,
                LoogaShaderPropertyType.Float => shaderPropertyType == ShaderUtil.ShaderPropertyType.Float,
                LoogaShaderPropertyType.Range => shaderPropertyType == ShaderUtil.ShaderPropertyType.Range,
                LoogaShaderPropertyType.Texture => shaderPropertyType == ShaderUtil.ShaderPropertyType.TexEnv,
                _ => true
            };
        }
    }
}
