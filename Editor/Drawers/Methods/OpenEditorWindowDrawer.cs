using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(OpenEditorWindowAttribute))]
    public sealed class OpenEditorWindowDrawer : PropertyDrawerBase
    {
        private const float ButtonWidth = 82f;
        private const float Gap = 4f;

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            OpenEditorWindowAttribute openAttribute = (OpenEditorWindowAttribute)attribute;
            Rect fieldRect = position;
            fieldRect.width -= ButtonWidth + Gap;

            Rect buttonRect = position;
            buttonRect.x = fieldRect.xMax + Gap;
            buttonRect.width = ButtonWidth;

            EditorGUI.PropertyField(fieldRect, property, label, true);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(openAttribute.MenuPath)))
            {
                if (GUI.Button(buttonRect, openAttribute.Label))
                    EditorApplication.ExecuteMenuItem(openAttribute.MenuPath);
            }
        }
    }
}
