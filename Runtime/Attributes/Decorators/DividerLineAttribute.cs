using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class DividerLineAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly EditorColor color;
        public readonly float thickness;
        public readonly float spacing;
        
        public DividerLineAttribute(EditorColor color = EditorColor.Gray, float thickness = 1.5f, float spacing = 20f)
        {
            this.color = color;
            this.thickness = thickness;
            this.spacing = spacing;
        }
    }
}