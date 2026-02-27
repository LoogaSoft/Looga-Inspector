using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class CustomLabelAttribute : PropertyAttribute, ILoogaAttribute
    {
        public string label;
        
        public CustomLabelAttribute(string label)
        {
            this.label = label;
        }
    }
}