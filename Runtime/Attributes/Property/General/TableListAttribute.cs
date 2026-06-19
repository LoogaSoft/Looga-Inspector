using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class TableListAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string[] columns;
        public readonly bool allowAddRemove;

        public TableListAttribute(params string[] columns)
        {
            this.columns = columns;
            allowAddRemove = true;
        }

        public TableListAttribute(bool allowAddRemove, params string[] columns)
        {
            this.columns = columns;
            this.allowAddRemove = allowAddRemove;
        }
    }
}
