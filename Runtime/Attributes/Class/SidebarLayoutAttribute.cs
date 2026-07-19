using System;

namespace LoogaSoft.Inspector.Runtime
{
    /// <summary>
    /// Draws serialized fields in a left-navigation layout using their
    /// <see cref="SidebarSectionAttribute"/> declarations.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class SidebarLayoutAttribute : Attribute
    {
    }
}
