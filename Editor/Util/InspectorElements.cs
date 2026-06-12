using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;

namespace LoogaSoft.Inspector.Editor
{
    public class InspectorElement
    {
        public string propertyName;
        public bool inTabGroup;
        public string tabName;
        public bool inFoldoutGroup;
        public string foldoutGroupName;
        public LoogaFoldoutStyle foldoutStyle;
        public bool foldoutDefaultExpanded;
        public bool endsFoldoutGroup;

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
            inFoldoutGroup = !string.IsNullOrWhiteSpace(groupName);
            foldoutGroupName = groupName;
            foldoutStyle = style;
            foldoutDefaultExpanded = defaultExpanded;
            endsFoldoutGroup = endsGroup;
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
