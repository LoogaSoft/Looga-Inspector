using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(ExposeScriptableAttribute))]
    public class ExposeScriptableDrawer : PropertyDrawerBase
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private UnityEditor.Editor _editor;
        
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            bool objectValid = property.objectReferenceValue != null;

            float indentOffset = EditorGUI.indentLevel * 15f;
            float labelWidth = EditorGUIUtility.labelWidth - indentOffset;

            if (objectValid)
            {
                Rect foldoutRect = new Rect(position.x, position.y, labelWidth, LineHeight);
                property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
            }
            else
                property.isExpanded = false;
            
            Rect fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, LineHeight);
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
            
            if (property.isExpanded && objectValid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                
                UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                
                _editor?.OnInspectorGUI();
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
            }
            
            EditorGUI.EndProperty();
        }
        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }
    }
}