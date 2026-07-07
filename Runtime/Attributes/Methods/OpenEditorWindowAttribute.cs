using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class OpenEditorWindowAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string Label;
        public readonly string MenuPath;

        public OpenEditorWindowAttribute(string menuPath)
            : this("Open", menuPath)
        {
        }

        public OpenEditorWindowAttribute(string label, string menuPath)
        {
            Label = string.IsNullOrWhiteSpace(label) ? "Open" : label;
            MenuPath = menuPath;
        }
    }
}
