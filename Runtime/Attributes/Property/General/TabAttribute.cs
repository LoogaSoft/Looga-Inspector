using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TabAttribute : Attribute, ILoogaAttribute
    {
        public readonly string tabName;
        public readonly int level;

        public TabAttribute(string tabName, int level = 0)
        {
            this.tabName = tabName;
            this.level = Math.Max(0, level);
        }
    }
    [AttributeUsage(AttributeTargets.All)]
    public class TabEndAttribute : Attribute, ILoogaAttribute
    {
    }
}
