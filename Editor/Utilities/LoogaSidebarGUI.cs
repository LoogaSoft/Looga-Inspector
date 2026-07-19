using System;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>Shared, pixel-stable rendering for Looga sidebar workspaces.</summary>
    public static class LoogaSidebarGUI
    {
        public const float DefaultWidth = 184f;
        public const float DefaultRowHeight = 42f;
        public const float DividerWidth = 1f;
        public const float ContentPadding = 10f;

        private static GUIStyle _buttonStyle;
        private static GUIStyle _headerStyle;

        public static GUIStyle HeaderStyle => _headerStyle ??= CreateHeaderStyle();

        public static int Navigation(
            Rect rect,
            Vector2 scroll,
            int selectedIndex,
            int itemCount,
            Func<int, string> getLabel,
            out Vector2 nextScroll)
        {
            EnsureStyles();
            EditorGUI.DrawRect(rect, LoogaEditorStyle.BoxColor);

            float contentHeight = itemCount * DefaultRowHeight + 1f;
            Rect contentRect = new(0f, 0f, rect.width, Mathf.Max(rect.height, contentHeight));
            nextScroll = GUI.BeginScrollView(rect, scroll, contentRect, false, false);

            int nextSelection = selectedIndex;
            Event current = Event.current;
            for (int i = 0; i < itemCount; i++)
            {
                Rect row = LoogaEditorStyle.PixelSnap(new Rect(0f, i * DefaultRowHeight, rect.width, DefaultRowHeight));
                bool selected = i == selectedIndex;
                bool hovered = row.Contains(current.mousePosition);
                EditorGUI.DrawRect(row, selected
                    ? LoogaEditorStyle.AlternateBoxColor
                    : hovered ? LoogaEditorStyle.HoverColor : LoogaEditorStyle.BoxColor);

                if (selected)
                {
                    EditorGUI.DrawRect(
                        new Rect(row.x, row.y, LoogaEditorStyle.AccentRailWidth, row.height),
                        LoogaEditorStyle.ActionAccentColor);
                }

                GUI.Label(new Rect(row.x + 14f, row.y, row.width - 22f, row.height), getLabel(i), _buttonStyle);
                EditorGUI.DrawRect(
                    new Rect(row.x, row.yMax - LoogaEditorStyle.Pixels(1f), row.width, LoogaEditorStyle.Pixels(1f)),
                    LoogaEditorStyle.SeparatorColor);

                if (current.type == EventType.MouseDown && current.button == 0 && hovered)
                {
                    nextSelection = i;
                    current.Use();
                }
            }

            EditorGUI.DrawRect(
                new Rect(0f, 0f, LoogaEditorStyle.Pixels(1f), contentRect.height),
                LoogaEditorStyle.SeparatorColor);
            EditorGUI.DrawRect(
                new Rect(rect.width - LoogaEditorStyle.Pixels(1f), 0f, LoogaEditorStyle.Pixels(1f), contentRect.height),
                LoogaEditorStyle.SeparatorColor);
            GUI.EndScrollView();
            return nextSelection;
        }

        public static void Divider(Rect rect)
        {
            EditorGUI.DrawRect(rect, LoogaEditorStyle.SeparatorColor);
        }

        private static void EnsureStyles()
        {
            if (_buttonStyle != null)
                return;

            _buttonStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(),
                margin = new RectOffset(),
                normal = { textColor = LoogaEditorStyle.TextColor }
            };
        }

        private static GUIStyle CreateHeaderStyle()
        {
            return new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset((int)ContentPadding, 0, 0, 0),
                normal = { textColor = LoogaEditorStyle.TextColor }
            };
        }
    }
}
