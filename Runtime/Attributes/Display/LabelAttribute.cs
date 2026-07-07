using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class LabelAttribute : Attribute, ILoogaAttribute
    {
        public readonly string label;

        public LabelAttribute(string label)
        {
            this.label = label;
        }
    }
}