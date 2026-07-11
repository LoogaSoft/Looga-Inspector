using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class LoogaEditorTabs
    {
        private const float TabHeight = 24f;
        private const float TabRowGap = 2f;
        private const float TabTextPadding = 24f;
        private const float TabSeparatorWidth = 1f;
        private const float SelectedAccentHeight = 2f;

        private static readonly Dictionary<string, float> ToolbarWidthCache = new();
        private static GUIStyle _tabLabelStyle;

        public static int DrawWrappingToolbar(int selectedIndex, string[] tabNames, string cacheKey)
        {
            if (tabNames == null || tabNames.Length == 0)
                return selectedIndex;

            selectedIndex = Mathf.Clamp(selectedIndex, 0, tabNames.Length - 1);
            float availableWidth = GetCachedWidth(cacheKey, EditorGUIUtility.currentViewWidth);
            List<List<int>> rows = BuildRows(tabNames, availableWidth);
            float totalHeight = GetRowsHeight(rows.Count);
            Rect fullRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(totalHeight), GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.Repaint)
                ToolbarWidthCache[cacheKey] = fullRect.width;

            return DrawRows(fullRect, rows, tabNames, selectedIndex);
        }

        public static int DrawWrappingToolbarWithRightControl(
            int selectedIndex,
            string[] tabNames,
            string cacheKey,
            float rightControlWidth,
            float rightControlGap,
            System.Action drawRightControl)
        {
            if (tabNames == null || tabNames.Length == 0)
                return selectedIndex;

            selectedIndex = Mathf.Clamp(selectedIndex, 0, tabNames.Length - 1);
            float fullWidth = GetCachedWidth(cacheKey, EditorGUIUtility.currentViewWidth);
            float reservedWidth = drawRightControl != null ? rightControlWidth + rightControlGap : 0f;
            float toolbarWidth = Mathf.Max(1f, fullWidth - reservedWidth);
            List<List<int>> rows = BuildRows(tabNames, toolbarWidth);
            float totalHeight = GetRowsHeight(rows.Count);

            Rect fullRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(totalHeight), GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint)
                ToolbarWidthCache[cacheKey] = fullRect.width;

            Rect tabsRect = new(fullRect.x, fullRect.y, Mathf.Max(1f, fullRect.width - reservedWidth), fullRect.height);
            int newSelectedIndex = DrawRows(tabsRect, rows, tabNames, selectedIndex);

            if (drawRightControl != null)
            {
                Rect controlRect = new(fullRect.xMax - rightControlWidth, fullRect.y, rightControlWidth, TabHeight);
                GUILayout.BeginArea(controlRect);
                drawRightControl();
                GUILayout.EndArea();
            }

            return newSelectedIndex;
        }

        private static int DrawRows(Rect fullRect, List<List<int>> rows, string[] tabNames, int selectedIndex)
        {
            EnsureStyles();
            if (Event.current.type == EventType.MouseMove && fullRect.Contains(Event.current.mousePosition))
                HandleUtility.Repaint();

            int newSelectedIndex = selectedIndex;

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                Rect rowRect = PixelSnap(new Rect(
                    fullRect.x,
                    fullRect.y + rowIndex * (TabHeight + TabRowGap),
                    fullRect.width,
                    TabHeight));
                List<int> row = rows[rowIndex];
                DrawRowBackground(rowRect);

                if (row.Count == 0)
                    continue;

                float tabWidth = rowRect.width / row.Count;
                for (int localIndex = 0; localIndex < row.Count; localIndex++)
                {
                    int tabIndex = row[localIndex];
                    Rect tabRect = PixelSnap(new Rect(
                        rowRect.x + localIndex * tabWidth,
                        rowRect.y,
                        localIndex == row.Count - 1 ? rowRect.xMax - (rowRect.x + localIndex * tabWidth) : tabWidth,
                        rowRect.height));
                    bool selected = tabIndex == selectedIndex;
                    bool hovered = tabRect.Contains(Event.current.mousePosition);

                    if (hovered && Event.current.type == EventType.MouseMove)
                        HandleUtility.Repaint();

                    DrawTab(tabRect, tabNames[tabIndex], selected, hovered);

                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && hovered)
                    {
                        newSelectedIndex = tabIndex;
                        Event.current.Use();
                    }
                }
            }

            return newSelectedIndex;
        }

        private static void DrawRowBackground(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            EditorGUI.DrawRect(rect, GetTabBarColor());
        }

        private static void DrawTab(Rect rect, string label, bool selected, bool hovered)
        {
            if (Event.current.type == EventType.Repaint)
            {
                Color color = selected ? GetSelectedTabColor() : GetTabColor();
                if (hovered && !selected)
                    color = Color.Lerp(color, GetHoverTabColor(), 0.75f);

                EditorGUI.DrawRect(rect, color);

                Rect separatorRect = PixelSnap(new Rect(rect.xMax - Pixels(TabSeparatorWidth), rect.y, Pixels(TabSeparatorWidth), rect.height));
                EditorGUI.DrawRect(separatorRect, GetSeparatorColor());

                if (selected)
                {
                    Rect accentRect = PixelSnap(new Rect(rect.x, rect.yMax - Pixels(SelectedAccentHeight), rect.width, Pixels(SelectedAccentHeight)));
                    EditorGUI.DrawRect(accentRect, GetAccentColor());
                }
            }

            Color previousColor = GUI.color;
            GUI.color = selected ? GetSelectedTextColor() : GetTextColor();
            GUI.Label(rect, label, _tabLabelStyle);
            GUI.color = previousColor;
        }

        private static List<List<int>> BuildRows(string[] tabNames, float availableWidth)
        {
            EnsureStyles();

            float[] minWidths = new float[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                Vector2 size = _tabLabelStyle.CalcSize(PropertyUtils.GetContent(tabNames[i]));
                minWidths[i] = Mathf.Ceil(size.x + TabTextPadding);
            }

            var rows = new List<List<int>>();
            var currentRow = new List<int>();
            float rowMaxWidth = 0f;

            for (int i = 0; i < tabNames.Length; i++)
            {
                float nextMaxWidth = Mathf.Max(rowMaxWidth, minWidths[i]);
                int nextCount = currentRow.Count + 1;
                bool rowWouldFit = nextMaxWidth * nextCount <= availableWidth;

                if (currentRow.Count > 0 && !rowWouldFit)
                {
                    rows.Add(currentRow);
                    currentRow = new List<int>();
                    rowMaxWidth = 0f;
                }

                currentRow.Add(i);
                rowMaxWidth = Mathf.Max(rowMaxWidth, minWidths[i]);
            }

            if (currentRow.Count > 0)
                rows.Add(currentRow);

            return rows;
        }

        private static float GetCachedWidth(string cacheKey, float fallbackWidth)
        {
            return ToolbarWidthCache.TryGetValue(cacheKey, out float width) && width > 0f
                ? width
                : Mathf.Max(1f, fallbackWidth - 40f);
        }

        private static float GetRowsHeight(int rowCount)
        {
            if (rowCount <= 0)
                return 0f;

            return rowCount * TabHeight + (rowCount - 1) * TabRowGap;
        }

        private static void EnsureStyles()
        {
            if (_tabLabelStyle != null)
                return;

            _tabLabelStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                clipping = TextClipping.Clip,
                fontSize = EditorStyles.label.fontSize,
                fontStyle = FontStyle.Normal,
                padding = new RectOffset(6, 6, 0, 1)
            };
        }

        private static Rect PixelSnap(Rect rect)
        {
            return Rect.MinMaxRect(
                PixelSnapValue(rect.xMin),
                PixelSnapValue(rect.yMin),
                PixelSnapValue(rect.xMax),
                PixelSnapValue(rect.yMax));
        }

        private static float PixelSnapValue(float value)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Round(value * pixelsPerPoint) / pixelsPerPoint;
        }

        private static float Pixels(float pixelCount)
        {
            return pixelCount / EditorGUIUtility.pixelsPerPoint;
        }

        private static Color GetTabBarColor()
        {
            return LoogaEditorStyle.TabBarColor;
        }

        private static Color GetTabColor()
        {
            return LoogaEditorStyle.TabColor;
        }

        private static Color GetSelectedTabColor()
        {
            return LoogaEditorStyle.SelectedTabColor;
        }

        private static Color GetHoverTabColor()
        {
            return LoogaEditorStyle.TabHoverColor;
        }

        private static Color GetSeparatorColor()
        {
            return LoogaEditorStyle.SeparatorColor;
        }

        private static Color GetAccentColor()
        {
            return LoogaEditorStyle.ActionAccentColor;
        }

        private static Color GetTextColor()
        {
            return LoogaEditorStyle.TextColor;
        }

        private static Color GetSelectedTextColor()
        {
            return LoogaEditorStyle.TextColor;
        }
    }
}