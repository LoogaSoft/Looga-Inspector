using System;

namespace LoogaSoft.Inspector.Runtime
{
    public enum LoogaFoldoutStyle
    {
        Small,
        Large
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaFoldoutAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Title;
        public readonly LoogaFoldoutStyle Style;
        public readonly bool DefaultExpanded;

        public LoogaFoldoutAttribute(
            string title = null,
            LoogaFoldoutStyle style = LoogaFoldoutStyle.Small,
            bool defaultExpanded = false)
        {
            Title = title;
            Style = style;
            DefaultExpanded = defaultExpanded;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaFoldoutGroupAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Title;
        public readonly LoogaFoldoutStyle Style;
        public readonly bool DefaultExpanded;

        public LoogaFoldoutGroupAttribute(
            string title,
            LoogaFoldoutStyle style = LoogaFoldoutStyle.Small,
            bool defaultExpanded = true)
        {
            Title = title;
            Style = style;
            DefaultExpanded = defaultExpanded;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaFoldoutGroupEndAttribute : Attribute, ILoogaAttribute
    {
    }
}
