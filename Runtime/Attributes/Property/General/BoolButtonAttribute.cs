using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class BoolButtonAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string methodName;
        public readonly string buttonLabel;

        public BoolButtonAttribute(string methodName)
        {
            this.methodName = methodName;
            buttonLabel = methodName;
        }

        public BoolButtonAttribute(string methodName, string buttonLabel)
        {
            this.methodName = methodName;
            this.buttonLabel = buttonLabel;
        }
    }
}