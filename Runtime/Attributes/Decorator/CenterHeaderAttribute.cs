using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class CenterHeaderAttribute : PropertyAttribute
    {
        public readonly string headerText;
        public readonly float height;
        public readonly float thickness;
        
        public CenterHeaderAttribute(string headerText, float height = 24f, float thickness = 1f)
        {
            this.headerText = headerText;
            this.height = height;
            this.thickness = thickness;
        }
    }
}