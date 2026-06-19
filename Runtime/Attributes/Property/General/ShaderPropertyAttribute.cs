using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public enum LoogaShaderPropertyType
    {
        Any,
        Color,
        Vector,
        Float,
        Range,
        Texture
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShaderPropertyAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string materialOrShaderMember;
        public readonly LoogaShaderPropertyType propertyType;

        public ShaderPropertyAttribute(
            string materialOrShaderMember,
            LoogaShaderPropertyType propertyType = LoogaShaderPropertyType.Any)
        {
            this.materialOrShaderMember = materialOrShaderMember;
            this.propertyType = propertyType;
        }
    }
}
