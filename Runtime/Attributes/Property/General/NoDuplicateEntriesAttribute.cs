using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class NoDuplicateEntriesAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string memberName;
        public readonly string message;

        public NoDuplicateEntriesAttribute(
            string memberName = null,
            string message = "This list contains duplicate entries.")
        {
            this.memberName = memberName;
            this.message = message;
        }
    }
}
