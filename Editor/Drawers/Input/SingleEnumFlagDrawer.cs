using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(SingleEnumFlagAttribute))]
    public class SingleEnumFlagDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Enum)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            int selectedIndex = Mathf.Clamp(property.enumValueIndex, 0, property.enumDisplayNames.Length - 1);
            int nextIndex = LoogaGUI.Popup(position, label, selectedIndex, property.enumDisplayNames);
            if (nextIndex != selectedIndex)
                property.enumValueIndex = nextIndex;
        }
    }
}