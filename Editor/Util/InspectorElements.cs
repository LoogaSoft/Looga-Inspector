using System.Collections.Generic;

namespace LoogaSoft.Inspector.Editor
{
    public class InspectorElement
    {
        public string propertyName;
        public bool inTabGroup;
        public string tabName;

        public InspectorElement(string propertyName, bool inTabGroup = false) : this(propertyName, inTabGroup, "")
        {
        }

        public InspectorElement(string propertyName, bool inTabGroup, string tabName = "")
        {
            this.propertyName = propertyName;
            this.inTabGroup = inTabGroup;
            this.tabName = tabName;
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