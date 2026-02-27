using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(CustomLabelAttribute))]
    public class CustomLabelDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            CustomLabelAttribute attr = (CustomLabelAttribute)attribute;
            
            GUIContent newLabel = new GUIContent(attr.label);
            
            EditorGUI.PropertyField(position, property, newLabel);
        }
    }
}