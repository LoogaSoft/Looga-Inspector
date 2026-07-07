using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssetDisplayNameAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string useCustomNameMember;

        public AssetDisplayNameAttribute(string useCustomNameMember = "_useCustomDisplayName")
        {
            this.useCustomNameMember = useCustomNameMember;
        }
    }
}
