using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class FittedTextAttribute : PropertyAttribute, ILoogaAttribute
    {
        public int minimumLines;
        
        public FittedTextAttribute(int minimumLines = 3)
        {
            this.minimumLines = minimumLines;
        }
    }
}