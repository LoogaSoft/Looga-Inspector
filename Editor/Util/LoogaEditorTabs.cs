using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class LoogaEditorTabs
    {
        private static readonly Dictionary<string, float> ToolbarWidthCache = new();

        /// <summary>
        /// Draws a toolbar that keeps Unity's native GUILayout.Toolbar styling while wrapping
        /// long tab groups onto extra rows before labels clip.
        /// </summary>
        public static int DrawWrappingToolbar(int selectedIndex, string[] tabNames, string cacheKey)
        {
            if (tabNames == null || tabNames.Length == 0)
                return selectedIndex;

            selectedIndex = Mathf.Clamp(selectedIndex, 0, tabNames.Length - 1);

            ToolbarWidthCache.TryGetValue(cacheKey, out float availableWidth);
            if (availableWidth <= 0f)
                availableWidth = float.MaxValue;

            List<List<int>> rows = BuildRows(tabNames, availableWidth);

            int newSelectedIndex = selectedIndex;
            bool firstRow = true;

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                List<int> row = rows[rowIndex];
                string[] rowLabels = new string[row.Count];
                for (int r = 0; r < row.Count; r++)
                    rowLabels[r] = tabNames[row[r]];

                int localSelected = -1;
                for (int r = 0; r < row.Count; r++)
                {
                    if (row[r] == selectedIndex)
                    {
                        localSelected = r;
                        break;
                    }
                }

                int localResult = GUILayout.Toolbar(localSelected, rowLabels, GUILayout.ExpandWidth(true));

                if (firstRow && Event.current.type == EventType.Repaint)
                {
                    Rect lastRect = GUILayoutUtility.GetLastRect();
                    ToolbarWidthCache[cacheKey] = lastRect.width;
                    firstRow = false;
                }

                if (localResult >= 0 && localResult != localSelected)
                    newSelectedIndex = row[localResult];

                if (rowIndex < rows.Count - 1)
                    GUILayout.Space(-EditorGUIUtility.standardVerticalSpacing);
            }

            return newSelectedIndex;
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

            ToolbarWidthCache.TryGetValue(cacheKey, out float fullWidth);
            if (fullWidth <= 0f)
                fullWidth = EditorGUIUtility.currentViewWidth;

            float reservedWidth = drawRightControl != null ? rightControlWidth + rightControlGap : 0f;
            float toolbarWidth = Mathf.Max(1f, fullWidth - reservedWidth);
            List<List<int>> rows = BuildRows(tabNames, toolbarWidth);

            int newSelectedIndex = selectedIndex;
            bool firstRow = true;

            for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    List<int> row = rows[rowIndex];
                    int localSelected = GetLocalSelected(row, selectedIndex);
                    int localResult = GUILayout.Toolbar(localSelected, GetRowLabels(row, tabNames), GUILayout.Width(toolbarWidth));

                    if (firstRow && Event.current.type == EventType.Repaint)
                    {
                        Rect lastRect = GUILayoutUtility.GetLastRect();
                        ToolbarWidthCache[cacheKey] = lastRect.width + reservedWidth;
                        firstRow = false;
                    }

                    if (localResult >= 0 && localResult != localSelected)
                        newSelectedIndex = row[localResult];

                    if (drawRightControl != null)
                    {
                        GUILayout.Space(rightControlGap);

                        if (rowIndex == 0)
                            drawRightControl();
                        else
                            GUILayout.Space(rightControlWidth);
                    }
                }

                if (rowIndex < rows.Count - 1)
                    GUILayout.Space(-EditorGUIUtility.standardVerticalSpacing);
            }

            return newSelectedIndex;
        }

        private static List<List<int>> BuildRows(string[] tabNames, float availableWidth)
        {
            GUIStyle buttonStyle = EditorStyles.toolbarButton;

            float[] minWidths = new float[tabNames.Length];
            for (int i = 0; i < tabNames.Length; i++)
            {
                Vector2 size = buttonStyle.CalcSize(new GUIContent(tabNames[i]));
                minWidths[i] = Mathf.Ceil(size.x + 16f);
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

        private static string[] GetRowLabels(List<int> row, string[] tabNames)
        {
            string[] rowLabels = new string[row.Count];
            for (int r = 0; r < row.Count; r++)
                rowLabels[r] = tabNames[row[r]];

            return rowLabels;
        }

        private static int GetLocalSelected(List<int> row, int selectedIndex)
        {
            for (int r = 0; r < row.Count; r++)
                if (row[r] == selectedIndex)
                    return r;

            return -1;
        }
    }
}
