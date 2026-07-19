using System;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>Registers an editor page with a named <see cref="LoogaSidebarWindow"/> workspace.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LoogaSidebarPageAttribute : Attribute
    {
        public LoogaSidebarPageAttribute(string workspaceId)
        {
            WorkspaceId = workspaceId;
        }

        public string WorkspaceId { get; }
    }
}
