using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FilteredEnumAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string ProviderMemberName;

        public FilteredEnumAttribute(string providerMemberName)
        {
            ProviderMemberName = providerMemberName;
        }
    }
}
