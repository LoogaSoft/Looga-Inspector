using System;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Layout-based Looga Inspector controls for custom inspectors and editor windows.
    /// Use this like EditorGUILayout/GUILayout when you do not manually own Rect layout.
    /// </summary>
    public static class LoogaGUILayout
    {
        public static int Tabs(int selectedIndex, string[] tabNames, string controlId)
        {
            return LoogaEditorTabs.DrawWrappingToolbar(selectedIndex, tabNames, controlId);
        }

        public static int Tabs(
            int selectedIndex,
            string[] tabNames,
            string controlId,
            float rightControlWidth,
            float rightControlGap,
            Action drawRightControl)
        {
            return LoogaEditorTabs.DrawWrappingToolbarWithRightControl(
                selectedIndex,
                tabNames,
                controlId,
                rightControlWidth,
                rightControlGap,
                drawRightControl);
        }

        public static void FoldoutLarge(string title, string stateKey, bool defaultExpanded, Action content)
        {
            LoogaEditorFoldouts.LoogaFoldoutLarge(title, stateKey, defaultExpanded, content);
        }

        public static bool FoldoutSmall(GUIContent label, bool expanded, Action content, SerializedProperty property = null)
        {
            return LoogaEditorFoldouts.LoogaFoldoutSmall(label, expanded, content, property);
        }

        public static bool FoldoutSmall(string label, bool expanded, Action content, SerializedProperty property = null)
        {
            return FoldoutSmall(new GUIContent(label), expanded, content, property);
        }

        public static void BoxLarge(string title, Action content)
        {
            LoogaEditorFoldouts.LoogaBoxLarge(title, content);
        }

        public static void BoxSmall(GUIContent label, Action content)
        {
            LoogaEditorFoldouts.LoogaBoxSmall(label, content);
        }

        public static void BoxSmall(string label, Action content)
        {
            BoxSmall(new GUIContent(label), content);
        }
    }
}