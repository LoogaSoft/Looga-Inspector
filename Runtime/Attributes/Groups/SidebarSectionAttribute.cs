using System;

namespace LoogaSoft.Inspector.Runtime
{
    /// <summary>Places a serialized field in an ordered sidebar section.</summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SidebarSectionAttribute : Attribute
    {
        public SidebarSectionAttribute(string name, int order = 0)
        {
            Name = name;
            Order = order;
        }

        public string Name { get; }
        public int Order { get; }
    }
}
