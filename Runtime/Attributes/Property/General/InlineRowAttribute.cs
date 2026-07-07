using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InlineRowAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string RowId;
        public readonly float Width;

        public InlineRowAttribute(string rowId = null, float width = 1f)
        {
            RowId = rowId;
            Width = Mathf.Max(0.01f, width);
        }
    }
}
