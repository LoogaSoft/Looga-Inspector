using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>Shared, pixel-stable rendering for Looga sidebar workspaces.</summary>
    public static class LoogaSidebarGUI
    {
        public const float DefaultWidth = 184f;
        public const float DefaultRowHeight = 42f;
        public const float GroupRowHeight = 36f;
        public const float ChildRowHeight = 30f;
        public const float DividerWidth = 1f;
        public const float ContentPadding = 10f;

        private static GUIStyle _buttonStyle;
        private static GUIStyle _groupStyle;
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

            float contentHeight = Mathf.Max(rect.height, itemCount * DefaultRowHeight);
            bool needsVerticalScrollbar = contentHeight > rect.height;
            float scrollbarWidth = needsVerticalScrollbar ? GUI.skin.verticalScrollbar.fixedWidth : 0f;
            float contentWidth = Mathf.Max(1f, rect.width - scrollbarWidth);
            Rect contentRect = new(0f, 0f, contentWidth, contentHeight);
            nextScroll = GUI.BeginScrollView(
                rect,
                new Vector2(0f, scroll.y),
                contentRect,
                false,
                needsVerticalScrollbar,
                GUIStyle.none,
                GUI.skin.verticalScrollbar);

            int nextSelection = selectedIndex;
            Event current = Event.current;
            for (int i = 0; i < itemCount; i++)
            {
                Rect row = LoogaEditorStyle.PixelSnap(new Rect(0f, i * DefaultRowHeight, contentWidth, DefaultRowHeight));
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
                new Rect(contentWidth - LoogaEditorStyle.Pixels(1f), 0f, LoogaEditorStyle.Pixels(1f), contentRect.height),
                LoogaEditorStyle.SeparatorColor);
            GUI.EndScrollView();
            return nextSelection;
        }

        /// <summary>
        /// Draws independently expandable groups and selectable child pages.
        /// The caller owns expansion and selection state so the view survives normal repaint cycles.
        /// </summary>
        public static void AccordionNavigation(
            Rect rect,
            Vector2 scroll,
            IReadOnlyList<AccordionGroup> groups,
            string selectedItemId,
            out Vector2 nextScroll,
            out string nextSelectedItemId,
            out string toggledGroupId)
        {
            EnsureStyles();
            EditorGUI.DrawRect(rect, LoogaEditorStyle.BoxColor);

            float contentHeight = 0f;
            for (int i = 0; i < groups.Count; i++)
            {
                contentHeight += GroupRowHeight;
                if (groups[i].Expanded)
                    contentHeight += groups[i].Items.Count * ChildRowHeight;
            }

            contentHeight = Mathf.Max(rect.height, contentHeight);
            bool needsVerticalScrollbar = contentHeight > rect.height;
            float scrollbarWidth = needsVerticalScrollbar ? GUI.skin.verticalScrollbar.fixedWidth : 0f;
            float contentWidth = Mathf.Max(1f, rect.width - scrollbarWidth);
            Rect contentRect = new(0f, 0f, contentWidth, contentHeight);
            nextScroll = GUI.BeginScrollView(
                rect,
                new Vector2(0f, scroll.y),
                contentRect,
                false,
                needsVerticalScrollbar,
                GUIStyle.none,
                GUI.skin.verticalScrollbar);

            nextSelectedItemId = selectedItemId;
            toggledGroupId = string.Empty;
            Event current = Event.current;
            float y = 0f;

            for (int groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                AccordionGroup group = groups[groupIndex];
                Rect groupRect = LoogaEditorStyle.PixelSnap(new Rect(0f, y, contentWidth, GroupRowHeight));
                bool groupHovered = groupRect.Contains(current.mousePosition);
                EditorGUI.DrawRect(groupRect, groupHovered ? LoogaEditorStyle.HoverColor : LoogaEditorStyle.BoxColor);

                Rect foldoutRect = new(groupRect.x + 8f, groupRect.y, groupRect.width - 12f, groupRect.height);
                bool nextExpanded = EditorGUI.Foldout(
                    foldoutRect,
                    group.Expanded,
                    group.Label,
                    true,
                    _groupStyle);
                if (nextExpanded != group.Expanded)
                    toggledGroupId = group.Id;

                y += GroupRowHeight;
                if (!group.Expanded)
                    continue;

                float guideX = LoogaEditorStyle.PixelSnapValue(20f);
                float guideHeight = group.Items.Count * ChildRowHeight;
                if (guideHeight > 0f)
                {
                    EditorGUI.DrawRect(
                        new Rect(guideX, y, LoogaEditorStyle.Pixels(1f), guideHeight),
                        LoogaEditorStyle.TreeLineColor);
                }

                for (int itemIndex = 0; itemIndex < group.Items.Count; itemIndex++)
                {
                    AccordionItem item = group.Items[itemIndex];
                    Rect itemRect = LoogaEditorStyle.PixelSnap(new Rect(0f, y, contentWidth, ChildRowHeight));
                    bool selected = string.Equals(item.Id, selectedItemId, StringComparison.Ordinal);
                    bool hovered = itemRect.Contains(current.mousePosition);
                    EditorGUI.DrawRect(itemRect, selected
                        ? LoogaEditorStyle.AlternateBoxColor
                        : hovered ? LoogaEditorStyle.HoverColor : LoogaEditorStyle.BoxColor);

                    if (selected)
                    {
                        EditorGUI.DrawRect(
                            new Rect(itemRect.x, itemRect.y, LoogaEditorStyle.AccentRailWidth, itemRect.height),
                            LoogaEditorStyle.ActionAccentColor);
                    }

                    GUI.Label(
                        new Rect(itemRect.x + 32f, itemRect.y, itemRect.width - 40f, itemRect.height),
                        item.Label,
                        _buttonStyle);

                    if (current.type == EventType.MouseDown && current.button == 0 && hovered)
                    {
                        nextSelectedItemId = item.Id;
                        current.Use();
                    }

                    y += ChildRowHeight;
                }
            }

            EditorGUI.DrawRect(
                new Rect(contentWidth - LoogaEditorStyle.Pixels(1f), 0f, LoogaEditorStyle.Pixels(1f), contentRect.height),
                LoogaEditorStyle.SeparatorColor);
            GUI.EndScrollView();
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

            _groupStyle = new GUIStyle(EditorStyles.foldout)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(EditorStyles.foldout.padding.left, 0, 0, 0),
                margin = new RectOffset(),
                normal = { textColor = LoogaEditorStyle.TextColor },
                onNormal = { textColor = LoogaEditorStyle.TextColor },
                hover = { textColor = LoogaEditorStyle.TextColor },
                onHover = { textColor = LoogaEditorStyle.TextColor },
                focused = { textColor = LoogaEditorStyle.TextColor },
                onFocused = { textColor = LoogaEditorStyle.TextColor }
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

        public readonly struct AccordionGroup
        {
            public AccordionGroup(string id, string label, bool expanded, IReadOnlyList<AccordionItem> items)
            {
                Id = id;
                Label = label;
                Expanded = expanded;
                Items = items;
            }

            public string Id { get; }
            public string Label { get; }
            public bool Expanded { get; }
            public IReadOnlyList<AccordionItem> Items { get; }
        }

        public readonly struct AccordionItem
        {
            public AccordionItem(string id, string label)
            {
                Id = id;
                Label = label;
            }

            public string Id { get; }
            public string Label { get; }
        }
    }
}
