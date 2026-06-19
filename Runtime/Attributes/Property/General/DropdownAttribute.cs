using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public readonly struct DropdownOption
    {
        public readonly string Label;
        public readonly object Value;

        public DropdownOption(string label, object value)
        {
            Label = label;
            Value = value;
        }
    }

    public class DropdownAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string listOrArrayName;
        public readonly string labelMember;
        public readonly string valueMember;
        
        public DropdownAttribute(string listOrArrayName, string labelMember = null, string valueMember = null)
        {
            this.listOrArrayName = listOrArrayName;
            this.labelMember = labelMember;
            this.valueMember = valueMember;
        }
    }
}
