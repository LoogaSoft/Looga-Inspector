using System;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class ExpandedListAttribute : Attribute, ILoogaAttribute
    {
    }
}
