using System.Collections.Generic;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public class InspectorLayout
    {
        public readonly List<InspectorElement> elements = new();
        public readonly HashSet<string> propertyNames = new();
        public readonly Dictionary<string, InspectorPropertyMetadata> propertyMetadata = new();
        public readonly List<TabGroupDefinition> tabGroups = new();
        
        public List<InspectorButton> buttons = new();
        
        public bool HasTabs => tabGroups.Count > 0;

        public bool TryGetMetadata(string propertyName, out InspectorPropertyMetadata metadata)
        {
            return propertyMetadata.TryGetValue(propertyName, out metadata);
        }
    }
}