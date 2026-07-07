using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;

namespace LoogaSoft.Inspector.Editor
{
    public class InspectorElement
    {
        public string propertyName;
        public bool inTabGroup;
        public string tabName;
        public readonly List<string> tabPath = new();
        public bool inStyledGroup;
        public string styledGroupName;
        public LoogaFoldoutStyle styledGroupStyle;
        public bool styledGroupDefaultExpanded;
        public bool styledGroupIsFoldout;
        public bool styledGroupIsToggleFoldout;
        public bool endsStyledGroup;
        public InspectorPropertyMetadata metadata;

        public bool inFoldoutGroup => inStyledGroup && styledGroupIsFoldout;
        public string foldoutGroupName => styledGroupName;
        public LoogaFoldoutStyle foldoutStyle => styledGroupStyle;
        public bool foldoutDefaultExpanded => styledGroupDefaultExpanded;
        public bool endsFoldoutGroup => endsStyledGroup && styledGroupIsFoldout;

        public InspectorElement(string propertyName, bool inTabGroup = false) : this(propertyName, inTabGroup, string.Empty)
        {
        }

        public InspectorElement(string propertyName, bool inTabGroup, string tabName = "")
        {
            this.propertyName = propertyName;
            this.inTabGroup = inTabGroup;
            this.tabName = tabName;

            if (inTabGroup && !string.IsNullOrWhiteSpace(tabName))
                tabPath.Add(tabName);
        }

        public InspectorElement(string propertyName, IReadOnlyList<string> tabPath)
        {
            this.propertyName = propertyName;

            if (tabPath == null || tabPath.Count == 0)
                return;

            inTabGroup = true;
            this.tabPath.AddRange(tabPath);
            tabName = tabPath[tabPath.Count - 1];
        }

        public void SetMetadata(InspectorPropertyMetadata metadata)
        {
            this.metadata = metadata;
        }

        public void SetFoldoutGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool defaultExpanded,
            bool endsGroup)
        {
            SetStyledGroup(groupName, style, defaultExpanded, true, false, endsGroup);
        }

        public void SetBoxGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool endsGroup)
        {
            SetStyledGroup(groupName, style, true, false, false, endsGroup);
        }

        public void SetToggleFoldoutGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool endsGroup)
        {
            SetStyledGroup(groupName, style, false, true, true, endsGroup);
        }

        private void SetStyledGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool defaultExpanded,
            bool isFoldout,
            bool isToggleFoldout,
            bool endsGroup)
        {
            inStyledGroup = !string.IsNullOrWhiteSpace(groupName);
            styledGroupName = groupName;
            styledGroupStyle = style;
            styledGroupDefaultExpanded = defaultExpanded;
            styledGroupIsFoldout = isFoldout;
            styledGroupIsToggleFoldout = isToggleFoldout;
            endsStyledGroup = endsGroup;
        }
    }

}
