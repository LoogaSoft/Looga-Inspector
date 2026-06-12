using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;

namespace LoogaSoft.Inspector.Editor
{
    public class InspectorElement
    {
        public string propertyName;
        public bool inTabGroup;
        public string tabName;
        public bool inStyledGroup;
        public string styledGroupName;
        public LoogaFoldoutStyle styledGroupStyle;
        public bool styledGroupDefaultExpanded;
        public bool styledGroupIsFoldout;
        public bool endsStyledGroup;

        public bool inFoldoutGroup => inStyledGroup && styledGroupIsFoldout;
        public string foldoutGroupName => styledGroupName;
        public LoogaFoldoutStyle foldoutStyle => styledGroupStyle;
        public bool foldoutDefaultExpanded => styledGroupDefaultExpanded;
        public bool endsFoldoutGroup => endsStyledGroup && styledGroupIsFoldout;

        public InspectorElement(string propertyName, bool inTabGroup = false) : this(propertyName, inTabGroup, "")
        {
        }

        public InspectorElement(string propertyName, bool inTabGroup, string tabName = "")
        {
            this.propertyName = propertyName;
            this.inTabGroup = inTabGroup;
            this.tabName = tabName;
        }

        public void SetFoldoutGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool defaultExpanded,
            bool endsGroup)
        {
            SetStyledGroup(groupName, style, defaultExpanded, true, endsGroup);
        }

        public void SetBoxGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool endsGroup)
        {
            SetStyledGroup(groupName, style, true, false, endsGroup);
        }

        private void SetStyledGroup(
            string groupName,
            LoogaFoldoutStyle style,
            bool defaultExpanded,
            bool isFoldout,
            bool endsGroup)
        {
            inStyledGroup = !string.IsNullOrWhiteSpace(groupName);
            styledGroupName = groupName;
            styledGroupStyle = style;
            styledGroupDefaultExpanded = defaultExpanded;
            styledGroupIsFoldout = isFoldout;
            endsStyledGroup = endsGroup;
        }
    }

    public class TabGroup
    {
        public int currentTabIndex;
        public List<string> tabNames;

        public TabGroup(int currentTabIndex, List<string> tabNames)
        {
            this.currentTabIndex = currentTabIndex;
            this.tabNames = tabNames;
        }
    }
}
