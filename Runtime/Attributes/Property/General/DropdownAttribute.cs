using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class DropdownAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string listOrArrayName;
        
        public DropdownAttribute(string listOrArrayName)
        {
            this.listOrArrayName = listOrArrayName;
        }
    }
}