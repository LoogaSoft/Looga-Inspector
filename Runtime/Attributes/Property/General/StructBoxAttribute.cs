using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class StructBoxAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string Title;

        public StructBoxAttribute(string title = null)
        {
            Title = title;
        }
    }
}
