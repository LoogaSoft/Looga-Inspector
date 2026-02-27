using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(FittedTextAttribute))]
    public class FittedTextDrawer : PropertyDrawerBase
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing * 2f;
        private static readonly GUIStyle TextStyle = new(EditorStyles.textArea) { wordWrap = true };

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "FittedTextAttribute can only be used with strings");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);
            
            var labelRect = new Rect(position.x, position.y, position.width, LineHeight);
            EditorGUI.LabelField(labelRect, label);
            
            var textRect = new Rect(position.x, labelRect.y + LineHeight, position.width, position.height - LineHeight - Spacing);
            
            property.stringValue = EditorGUI.TextArea(textRect, property.stringValue, TextStyle);
            
            EditorGUI.EndProperty();
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String) 
                return GetPropertyHeight(property, label);
            
            var width = EditorGUIUtility.currentViewWidth;
            GUIContent textContent = new(property.stringValue);
            var height = TextStyle.CalcHeight(textContent, width);
            
            var textAttribute = (FittedTextAttribute)attribute;
            var minHeight = (TextStyle.lineHeight * textAttribute.minimumLines) + TextStyle.padding.vertical;
            var finalHeight = Mathf.Max(minHeight, height);
            
            return Spacing + LineHeight + finalHeight;
        }
    }
}