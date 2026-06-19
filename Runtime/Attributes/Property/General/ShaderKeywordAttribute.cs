using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ShaderKeywordAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string materialOrShaderMember;

        public ShaderKeywordAttribute(string materialOrShaderMember)
        {
            this.materialOrShaderMember = materialOrShaderMember;
        }
    }
}
