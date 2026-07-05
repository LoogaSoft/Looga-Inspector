using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaCatalogAttribute : PropertyAttribute, ILoogaAttribute
    {
        public LoogaCatalogAttribute(string title)
        {
            Title = title;
        }

        public string Title { get; }
        public string TreePath { get; set; }
        public string CreateName { get; set; }
        public bool StoreAsSubAssets { get; set; } = true;
        public bool AllowAdd { get; set; } = true;
        public bool AllowDelete { get; set; } = true;
    }
}
