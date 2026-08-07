using System;

namespace LoogaSoft.Inspector.Runtime
{
    /// <summary>
    /// Draws an array or list with Looga Inspector's collapsible list interface.
    /// Unmarked collections use Unity's standard list interface.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaListAttribute : Attribute, ILoogaAttribute
    {
    }
}
