using LoogaSoft.Inspector.Runtime;
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
        private const float StatusBoxPadding = 7f;
        private const float StatusActionSize = 18f;

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

        public static bool StatusBox(
            Rect position,
            string message,
            LoogaStatusBoxType type = LoogaStatusBoxType.Info,
            bool hasAction = false,
            string actionLabel = "",
            string actionTooltip = "Open")
        {
            Rect rect = LoogaEditorStyle.PixelSnap(position);
            EditorGUI.DrawRect(rect, LoogaEditorStyle.BoxColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, LoogaEditorStyle.AccentRailWidth, rect.height), GetStatusAccentColor(type));

            float actionWidth = 0f;
            if (hasAction)
            {
                actionWidth = string.IsNullOrWhiteSpace(actionLabel)
                    ? StatusActionSize
                    : Mathf.Min(150f, EditorStyles.miniButton.CalcSize(new GUIContent(actionLabel)).x + 16f);
            }

            Rect labelRect = new(
                rect.x + LoogaEditorStyle.AccentRailWidth + StatusBoxPadding,
                rect.y,
                Mathf.Max(0f, rect.width - LoogaEditorStyle.AccentRailWidth - StatusBoxPadding * 2f - actionWidth - (hasAction ? StatusBoxPadding : 0f)),
                rect.height);

            GUI.Label(labelRect, new GUIContent(message), GetStatusMessageStyle());

            if (!hasAction)
                return false;

            Rect actionRect = new(
                rect.xMax - StatusBoxPadding - actionWidth,
                rect.y + Mathf.Round((rect.height - StatusActionSize) * 0.5f),
                actionWidth,
                StatusActionSize);

            return string.IsNullOrWhiteSpace(actionLabel)
                ? DrawOpenActionButton(actionRect, actionTooltip)
                : DrawTextActionButton(actionRect, actionLabel, actionTooltip);
        }

        public static float GetStatusBoxHeight(string message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? 0f
                : EditorGUIUtility.singleLineHeight + StatusBoxPadding * 2f;
        }

        private static bool DrawTextActionButton(Rect rect, string label, string tooltip)
        {
            Event current = Event.current;
            bool hovered = rect.Contains(current.mousePosition);
            Color color = GUI.enabled
                ? hovered ? Brighten(LoogaEditorStyle.ActionAccentColor, 1.25f) : LoogaEditorStyle.ActionAccentColor
                : new Color(LoogaEditorStyle.BoxColor.r, LoogaEditorStyle.BoxColor.g, LoogaEditorStyle.BoxColor.b, 0.55f);

            if (current.type == EventType.Repaint)
                EditorGUI.DrawRect(LoogaEditorStyle.PixelSnap(rect), color);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            return GUI.Button(rect, new GUIContent(label, tooltip), GetStatusActionButtonStyle());
        }

        private static bool DrawOpenActionButton(Rect rect, string tooltip)
        {
            int controlId = GUIUtility.GetControlID("LoogaStatusBoxOpen".GetHashCode(), FocusType.Passive, rect);
            Event current = Event.current;
            bool hovered = rect.Contains(current.mousePosition);
            bool pressed = GUIUtility.hotControl == controlId;

            if (hovered)
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (current.type == EventType.Repaint)
            {
                Color color = pressed
                    ? Brighten(LoogaEditorStyle.SelectionColor, 1.08f)
                    : hovered
                        ? Brighten(LoogaEditorStyle.ActionAccentColor, 1.25f)
                        : LoogaEditorStyle.ActionAccentColor;
                Rect background = LoogaEditorStyle.PixelSnap(rect);
                EditorGUI.DrawRect(background, color);
                DrawOpenGlyph(background, hovered || pressed);
            }

            GUI.Label(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);

            if (current.type == EventType.MouseDown && current.button == 0 && hovered)
            {
                GUIUtility.hotControl = controlId;
                current.Use();
                return false;
            }

            if (current.type == EventType.MouseUp && GUIUtility.hotControl == controlId)
            {
                GUIUtility.hotControl = 0;
                current.Use();
                return hovered;
            }

            return false;
        }

        private static void DrawOpenGlyph(Rect rect, bool highlighted)
        {
            Color lineColor = highlighted ? Color.white : new Color(0.93f, 0.93f, 0.93f, 1f);
            Rect glyph = new(
                Mathf.Round(rect.x + 5f),
                Mathf.Round(rect.y + 5f),
                Mathf.Round(rect.width - 10f),
                Mathf.Round(rect.height - 10f));

            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = lineColor;

            Vector3 bottomLeft = new(glyph.xMin, glyph.yMax - 1f, 0f);
            Vector3 topLeft = new(glyph.xMin, glyph.yMin + 4f, 0f);
            Vector3 bottomRight = new(glyph.xMax - 4f, glyph.yMax - 1f, 0f);
            Vector3 topRight = new(glyph.xMax, glyph.yMin, 0f);
            Vector3 arrowStart = new(glyph.xMin + 4f, glyph.yMax - 5f, 0f);
            Vector3 arrowEnd = new(glyph.xMax, glyph.yMin, 0f);

            Handles.DrawAAPolyLine(1.5f, bottomLeft, topLeft, bottomLeft, bottomRight);
            Handles.DrawAAPolyLine(1.5f, arrowStart, arrowEnd);
            Handles.DrawAAPolyLine(1.5f,
                new Vector3(topRight.x - 4f, topRight.y, 0f),
                topRight,
                new Vector3(topRight.x, topRight.y + 4f, 0f));

            Handles.color = previous;
            Handles.EndGUI();
        }

        private static GUIStyle GetStatusMessageStyle()
        {
            GUIStyle style = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            style.normal.textColor = LoogaEditorStyle.TextColor;
            return style;
        }

        private static GUIStyle GetStatusActionButtonStyle()
        {
            GUIStyle style = new(EditorStyles.miniButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(6, 6, 0, 1)
            };
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            return style;
        }

        private static Color GetStatusAccentColor(LoogaStatusBoxType type)
        {
            return type switch
            {
                LoogaStatusBoxType.Warning => new Color(0.95f, 0.68f, 0.22f, 1f),
                LoogaStatusBoxType.Error => new Color(0.86f, 0.24f, 0.20f, 1f),
                _ => LoogaEditorStyle.ActionAccentColor
            };
        }

        private static Color Brighten(Color color, float multiplier)
        {
            return new Color(
                Mathf.Clamp01(color.r * multiplier),
                Mathf.Clamp01(color.g * multiplier),
                Mathf.Clamp01(color.b * multiplier),
                color.a);
        }
    }
}