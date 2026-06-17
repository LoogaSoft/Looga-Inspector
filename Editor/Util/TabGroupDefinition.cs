using System.Collections.Generic;

namespace LoogaSoft.Inspector.Editor
{
    public class TabGroupDefinition
    {
        public readonly List<string> tabNames = new();

        public void AddPath(IReadOnlyList<string> tabPath)
        {
            if (tabPath == null || tabPath.Count == 0)
                return;

            AddName(tabPath[0]);
        }

        private void AddName(string tabName)
        {
            if (string.IsNullOrWhiteSpace(tabName) || tabNames.Contains(tabName))
                return;

            tabNames.Add(tabName);
        }
    }
}
