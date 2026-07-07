using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class GlobalShaderPropertyAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly LoogaShaderPropertyType propertyType;

        public GlobalShaderPropertyAttribute(LoogaShaderPropertyType propertyType = LoogaShaderPropertyType.Any)
        {
            this.propertyType = propertyType;
        }
    }
}
