using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class StructBoxAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string Title;

        public StructBoxAttribute(string title = null)
        {
            Title = title;
        }
    }
}
