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
            property.intValue = LoogaGUI.Popup(position, label.text, property.intValue, property.enumDisplayNames);
        }
    }
}