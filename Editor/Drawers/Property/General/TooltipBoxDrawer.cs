using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(TooltipBoxAttribute))]
    public class TooltipBoxDrawer : PropertyDrawerBase
    {
        private static readonly float Spacing = EditorGUIUtility.standardVerticalSpacing;
        
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            TooltipBoxAttribute boxAttribute = (TooltipBoxAttribute)attribute;
            MessageType messageType = boxAttribute.type switch
            {
                TooltipType.Info => MessageType.Info,
                TooltipType.Warning => MessageType.Warning,
                TooltipType.Error => MessageType.Error,
                _ => MessageType.None
            };
            
            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            float boxHeight = GetBoxHeight(boxAttribute.tooltip);
            
            Rect boxRect = new(position.x, position.y, position.width, boxHeight);
            Rect propertyRect = new(position.x, position.y + boxHeight + Spacing, position.width, propertyHeight);
            
            EditorGUI.HelpBox(boxRect, boxAttribute.tooltip, messageType);
            EditorGUI.PropertyField(propertyRect, property, label, true);
            
            EditorGUI.EndProperty();
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            TooltipBoxAttribute boxAttribute = (TooltipBoxAttribute)attribute;
            
            float boxHeight = GetBoxHeight(boxAttribute.tooltip);
            float propertyHeight = EditorGUI.GetPropertyHeight(property, label, true);
            
            return boxHeight + propertyHeight + Spacing;
        }
        private float GetBoxHeight(string text)
        {
            GUIContent content = new(text);
            float height = EditorStyles.helpBox.CalcHeight(content, EditorGUIUtility.currentViewWidth);
            height = Mathf.Max(height, 38f);
            
            return height;
        }
    }
}