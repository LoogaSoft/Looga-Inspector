using System.Collections.Generic;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(ShaderKeywordAttribute))]
    public sealed class ShaderKeywordDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            ShaderKeywordAttribute keywordAttribute = (ShaderKeywordAttribute)attribute;
            Shader shader = ResolveShader(property, keywordAttribute.materialOrShaderMember);
            if (shader == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            List<string> keywords = GetKeywordNames(shader);
            if (keywords.Count == 0)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (!string.IsNullOrWhiteSpace(property.stringValue) && !keywords.Contains(property.stringValue))
                keywords.Insert(0, property.stringValue);

            int currentIndex = Mathf.Max(0, keywords.IndexOf(property.stringValue));
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, keywords.ToArray());
            property.stringValue = keywords[Mathf.Clamp(newIndex, 0, keywords.Count - 1)];
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

        private static List<string> GetKeywordNames(Shader shader)
        {
            List<string> names = new();
            LocalKeyword[] keywords = shader.keywordSpace.keywords;

            foreach (LocalKeyword keyword in keywords)
            {
                if (!string.IsNullOrWhiteSpace(keyword.name))
                    names.Add(keyword.name);
            }

            names.Sort();
            return names;
        }
    }
}
