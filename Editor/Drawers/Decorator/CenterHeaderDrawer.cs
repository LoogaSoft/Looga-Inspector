using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(CenterHeaderAttribute))]
    public class CenterHeaderDrawer : DecoratorDrawer
    {
        private readonly float heightPadding = 6f;
        private readonly float textPadding = 10f;
        
        public override float GetHeight()
        {
            var attr = (CenterHeaderAttribute)attribute;
            return attr.height + heightPadding;
        }

        public override void OnGUI(Rect position)
        {
            var attr = (CenterHeaderAttribute)attribute;

            GUIStyle style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12
            };

            GUIContent content = new GUIContent(attr.headerText);
            Vector2 textSize = style.CalcSize(content);

            Rect labelRect = position;
            labelRect.height = attr.height;

            labelRect.y += heightPadding / 2f;
            
            GUI.Label(labelRect, content, style);
            
            float centerX = position.x + (position.width / 2f);
            float lineY = position.y + (position.height / 2f);
            float lineWidth = (position.width - textSize.x - (textPadding * 2f)) / 2f;

            if (lineWidth > 0)
            {
                Color lineColor = Color.gray4;
                
                Rect leftLine = new Rect(position.x, lineY, lineWidth, attr.thickness);
                Rect rightLine = new Rect(centerX + (textSize.x / 2f) + textPadding, lineY, lineWidth, attr.thickness);
                
                EditorGUI.DrawRect(leftLine, lineColor);
                EditorGUI.DrawRect(rightLine, lineColor);
            }
        }
    }
}