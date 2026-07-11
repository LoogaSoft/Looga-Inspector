using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Rect-based Looga Inspector controls for PropertyDrawer and custom GUI code.
    /// Use this like EditorGUI/GUI when the caller owns exact positioning.
    /// </summary>
    public static class LoogaGUI
    {
        public static int Tabs(Rect position, int selectedIndex, string[] tabNames)
        {
            return LoogaEditorTabs.DrawToolbar(position, selectedIndex, tabNames);
        }

        public static float GetTabsHeight(string[] tabNames, float availableWidth)
        {
            return LoogaEditorTabs.GetToolbarHeight(tabNames, availableWidth);
        }

        public static bool FoldoutLarge(Rect position, GUIContent label, bool expanded, out Rect contentRect, SerializedProperty property = null)
        {
            return LoogaEditorFoldouts.LoogaFoldoutLarge(position, label, expanded, out contentRect, property);
        }

        public static bool FoldoutSmall(Rect position, GUIContent label, bool expanded, out Rect contentRect, SerializedProperty property = null)
        {
            return LoogaEditorFoldouts.LoogaFoldoutSmall(position, label, expanded, out contentRect, property);
        }

        public static bool FoldoutSmallHeader(Rect headerRect, GUIContent label, bool expanded, SerializedProperty property = null)
        {
            return LoogaEditorFoldouts.LoogaFoldoutSmallHeader(headerRect, label, expanded, property);
        }
    }
}