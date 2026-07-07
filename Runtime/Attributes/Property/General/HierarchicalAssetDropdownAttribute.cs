using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class HierarchicalAssetDropdownAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly Type AssetType;
        public readonly string PathMemberName;
        public readonly string SearchFilter;
        public readonly bool IncludeNone;

        public HierarchicalAssetDropdownAttribute(
            string pathMemberName = "Path",
            string searchFilter = null,
            bool includeNone = true)
            : this(null, pathMemberName, searchFilter, includeNone)
        {
        }

        public HierarchicalAssetDropdownAttribute(
            Type assetType,
            string pathMemberName = "Path",
            string searchFilter = null,
            bool includeNone = true)
        {
            AssetType = assetType;
            PathMemberName = pathMemberName;
            SearchFilter = searchFilter;
            IncludeNone = includeNone;
        }
    }
}
