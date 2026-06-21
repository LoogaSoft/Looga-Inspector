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
        private static GUIStyle _textStyle;

        private static GUIStyle TextStyle
        {
            get
            {
                _textStyle ??= new GUIStyle(EditorStyles.textArea) { wordWrap = true };
                return _textStyle;
            }
        }

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "FittedTextAttribute can only be used with strings");
                return;
            }

            EditorGUI.BeginProperty(position, label, property);

            Rect labelRect = new(position.x, position.y, position.width, LineHeight);
            EditorGUI.LabelField(labelRect, label);

            Rect textRect = new(position.x, labelRect.y + LineHeight, position.width, position.height - LineHeight - Spacing);
            property.stringValue = EditorGUI.TextArea(textRect, property.stringValue, TextStyle);

            EditorGUI.EndProperty();
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
                return EditorGUI.GetPropertyHeight(property, label, true);

            float width = Mathf.Max(1f, EditorGUIUtility.currentViewWidth);
            GUIContent textContent = new(property.stringValue);
            float height = TextStyle.CalcHeight(textContent, width);

            FittedTextAttribute textAttribute = (FittedTextAttribute)attribute;
            float minHeight = TextStyle.lineHeight * textAttribute.minimumLines + TextStyle.padding.vertical;
            float finalHeight = Mathf.Max(minHeight, height);

            return Spacing + LineHeight + finalHeight;
        }
    }
}
