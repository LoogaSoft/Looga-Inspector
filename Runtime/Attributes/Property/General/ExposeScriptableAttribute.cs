using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class ExposeScriptableAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly bool showScriptField;
        public readonly bool expandedByDefault;
        public readonly string createButtonLabel;

        public ExposeScriptableAttribute(
            bool showScriptField = true,
            bool expandedByDefault = false,
            string createButtonLabel = "Create")
        {
            this.showScriptField = showScriptField;
            this.expandedByDefault = expandedByDefault;
            this.createButtonLabel = createButtonLabel;
        }
    }
}
