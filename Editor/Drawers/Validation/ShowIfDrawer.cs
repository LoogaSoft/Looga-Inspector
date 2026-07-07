using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (PropertyUtils.IsVisible(property)) return EditorGUIUtility.singleLineHeight;
            
            return -EditorGUIUtility.standardVerticalSpacing;
        }
    }
}