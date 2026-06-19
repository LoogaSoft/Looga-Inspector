using System.Collections.Generic;
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
            Shader shader = LoogaShaderEditorUtility.ResolveShader(property, keywordAttribute.materialOrShaderMember);
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
