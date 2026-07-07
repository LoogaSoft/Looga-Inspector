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

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaToggleFoldoutAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Title;
        public readonly LoogaFoldoutStyle Style;
        public readonly string TogglePropertyName;

        public LoogaToggleFoldoutAttribute(
            string title = null,
            string togglePropertyName = null,
            LoogaFoldoutStyle style = LoogaFoldoutStyle.Large)
        {
            Title = title;
            TogglePropertyName = togglePropertyName;
            Style = style;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaToggleFoldoutGroupAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Title;
        public readonly LoogaFoldoutStyle Style;

        public LoogaToggleFoldoutGroupAttribute(
            string title,
            LoogaFoldoutStyle style = LoogaFoldoutStyle.Large)
        {
            Title = title;
            Style = style;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaToggleFoldoutGroupEndAttribute : Attribute, ILoogaAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaBoxAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Title;
        public readonly LoogaFoldoutStyle Style;

        public LoogaBoxAttribute(
            string title = null,
            LoogaFoldoutStyle style = LoogaFoldoutStyle.Small)
        {
            Title = title;
            Style = style;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaBoxGroupAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Title;
        public readonly LoogaFoldoutStyle Style;

        public LoogaBoxGroupAttribute(
            string title,
            LoogaFoldoutStyle style = LoogaFoldoutStyle.Small)
        {
            Title = title;
            Style = style;
        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class LoogaBoxGroupEndAttribute : Attribute, ILoogaAttribute
    {
    }
}

