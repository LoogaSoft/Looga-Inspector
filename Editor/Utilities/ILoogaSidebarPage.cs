using UnityEditor;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>Supplies one independently owned page to a Looga sidebar workspace.</summary>
    public interface ILoogaSidebarPage
    {
        string PageId { get; }
        string DisplayName { get; }
        string Description { get; }
        int Order { get; }

        void Attach(EditorWindow host);
        void DrawPage();
        void RefreshPage();
    }
}
