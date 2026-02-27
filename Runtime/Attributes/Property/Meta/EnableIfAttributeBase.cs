using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class EnableIfAttributeBase : Attribute, ILoogaAttribute
    {
        public readonly string condition;
        public bool inverted;
        
        public EnableIfAttributeBase(string condition)
        {
            this.condition = condition;
        }
    }
}