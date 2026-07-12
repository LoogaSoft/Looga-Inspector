using System.Collections.Generic;
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
            Shader shader = LoogaShaderEditorUtility.ResolveShader(property, shaderPropertyAttribute.materialOrShaderMember);
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
            int newIndex = LoogaGUI.Popup(position, label.text, currentIndex, propertyNames.ToArray());
            property.stringValue = propertyNames[Mathf.Clamp(newIndex, 0, propertyNames.Count - 1)];
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
