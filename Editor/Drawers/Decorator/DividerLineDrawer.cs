using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(DividerLineAttribute))]
    public class DividerLineDrawer : DecoratorDrawer
    {
        public override void OnGUI(Rect position)
        {
            DividerLineAttribute lineAttribute = (DividerLineAttribute)attribute;

            Rect lineRect = new Rect(position.x, position.y + lineAttribute.spacing / 2f, position.width, lineAttribute.thickness);
            
            EditorGUI.DrawRect(lineRect, lineAttribute.color.GetColor());
        }

        public override float GetHeight()
        {
            DividerLineAttribute lineAttribute = (DividerLineAttribute)attribute;
            return lineAttribute.thickness + lineAttribute.spacing + EditorGUIUtility.standardVerticalSpacing * 2f;
        }
    }
}