using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class OnFieldChangedAttribute : Attribute, ILoogaAttribute
    {
        public readonly string MethodName;

        public OnFieldChangedAttribute(string methodName)
        {
            this.MethodName = methodName;
        }
    }
}