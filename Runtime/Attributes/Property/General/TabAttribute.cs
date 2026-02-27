using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class TabAttribute : Attribute, ILoogaAttribute
    {
        public readonly string tabName;
        public TabAttribute(string tabName)
        {
            this.tabName = tabName;
        }
    }
    [AttributeUsage(AttributeTargets.All)]
    public class TabEndAttribute : Attribute, ILoogaAttribute
    {
    }
}