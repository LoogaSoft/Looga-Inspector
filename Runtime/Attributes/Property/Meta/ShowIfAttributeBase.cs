using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class ShowIfAttributeBase : Attribute, ILoogaAttribute
    {
        public readonly string condition;
        public bool inverted;
        
        public ShowIfAttributeBase(string condition)
        {
            this.condition = condition;
        }
    }
}