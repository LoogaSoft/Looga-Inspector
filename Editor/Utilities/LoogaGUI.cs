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
        private const float StatusBoxPadding = 2f;
        private const float StatusActionSize = 18f;
        private const string DefaultStatusActionLabel = "Open";
        private const float StatusIndicatorSize = 5f;

        private static Color StatusBackgroundColor => EditorGUIUtility.isProSkin
            ? new Color(0.225f, 0.225f, 0.225f, 1f)
            : new Color(0.79f, 0.79f, 0.79f, 1f);

        private static Color StatusActionColor => EditorGUIUtility.isProSkin
            ? new Color(0.155f, 0.155f, 0.155f, 1f)
            : new Color(0.70f, 0.70f, 0.70f, 1f);

        private static Color StatusActionHoverColor => EditorGUIUtility.isProSkin
            ? new Color(0.245f, 0.245f, 0.245f, 1f)
            : new Color(0.78f, 0.78f, 0.78f, 1f);

        private static Color StatusActionPressedColor => EditorGUIUtility.isProSkin
            ? new Color(0.115f, 0.115f, 0.115f, 1f)
            : new Color(0.62f, 0.62f, 0.62f, 1f);

        private static GUIStyle _dropdownStyle;
        private static Texture2D _dropdownNormalTexture;
        private static Texture2D _dropdownHoverTexture;
        private static Texture2D _dropdownActiveTexture;

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

        public static int Popup(Rect position, string label, int selectedIndex, string[] displayedOptions)
        {
            return Popup(position, new GUIContent(label), selectedIndex, displayedOptions);
        }

        public static int Popup(Rect position, GUIContent label, int selectedIndex, string[] displayedOptions)
        {
            GUIContent[] options = ToDropdownContent(displayedOptions);
            return Popup(position, label, selectedIndex, options);
        }

        public static int Popup(Rect position, GUIContent label, int selectedIndex, GUIContent[] displayedOptions)
        {
            Rect fieldRect = GetLabeledFieldRect(position, label);
            int result = EditorGUI.Popup(fieldRect, GUIContent.none, selectedIndex, displayedOptions, GetDropdownStyle());
            DrawDropdownArrow(fieldRect);
            return result;
        }

        public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions)
        {
            Rect fieldRect = GetLabeledFieldRect(position, label);
            int result = EditorGUI.MaskField(fieldRect, GUIContent.none, mask, displayedOptions, GetDropdownStyle());
            DrawDropdownArrow(fieldRect);
            return result;
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
            EditorGUI.DrawRect(rect, StatusBackgroundColor);

            Rect indicatorRect = new(
                rect.x + StatusBoxPadding + 1f,
                rect.y + Mathf.Round((rect.height - StatusIndicatorSize) * 0.5f),
                StatusIndicatorSize,
                StatusIndicatorSize);
            DrawStatusIndicator(indicatorRect, GetStatusAccentColor(type));

            if (hasAction && string.IsNullOrWhiteSpace(actionLabel))
                actionLabel = DefaultStatusActionLabel;

            float actionWidth = 0f;
            if (hasAction)
                actionWidth = Mathf.Min(150f, EditorStyles.miniButton.CalcSize(new GUIContent(actionLabel)).x + 14f);

            float labelX = indicatorRect.xMax + StatusBoxPadding + 3f;
            Rect labelRect = new(
                labelX,
                rect.y,
                Mathf.Max(0f, rect.xMax - labelX - StatusBoxPadding - actionWidth - (hasAction ? StatusBoxPadding : 0f)),
                rect.height);

            GUI.Label(labelRect, new GUIContent(message), GetStatusMessageStyle());

            if (!hasAction)
                return false;

            Rect actionRect = new(
                rect.xMax - StatusBoxPadding - actionWidth,
                rect.y + Mathf.Round((rect.height - StatusActionSize) * 0.5f),
                actionWidth,
                StatusActionSize);

            return DrawTextActionButton(actionRect, actionLabel, actionTooltip);
        }

        public static float GetStatusBoxHeight(string message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? 0f
                : Mathf.Ceil(EditorGUIUtility.singleLineHeight + StatusBoxPadding * 2f);
        }

        private static Rect GetLabeledFieldRect(Rect position, GUIContent label)
        {
            return label == null || label == GUIContent.none || string.IsNullOrEmpty(label.text)
                ? position
                : EditorGUI.PrefixLabel(position, label);
        }

        private static GUIContent[] ToDropdownContent(string[] options)
        {
            if (options == null || options.Length == 0)
                return new[] { GUIContent.none };

            GUIContent[] content = new GUIContent[options.Length];
            for (int i = 0; i < options.Length; i++)
                content[i] = new GUIContent(options[i]);

            return content;
        }

        private static GUIStyle GetDropdownStyle()
        {
            if (_dropdownStyle == null)
            {
                _dropdownStyle = new GUIStyle(EditorStyles.popup)
                {
                    alignment = TextAnchor.MiddleLeft,
                    clipping = TextClipping.Clip,
                    padding = new RectOffset(6, 18, 0, 0),
                    border = new RectOffset(0, 0, 0, 0),
                    margin = new RectOffset(0, 0, 0, 0),
                    overflow = new RectOffset(0, 0, 0, 0)
                };
            }

            _dropdownStyle.normal.background = GetDropdownTexture(ref _dropdownNormalTexture, LoogaEditorStyle.BoxColor);
            _dropdownStyle.hover.background = GetDropdownTexture(ref _dropdownHoverTexture, LoogaEditorStyle.HoverColor);
            _dropdownStyle.active.background = GetDropdownTexture(ref _dropdownActiveTexture, LoogaEditorStyle.TabBarColor);
            _dropdownStyle.focused.background = _dropdownStyle.normal.background;
            _dropdownStyle.normal.textColor = LoogaEditorStyle.TextColor;
            _dropdownStyle.hover.textColor = LoogaEditorStyle.TextColor;
            _dropdownStyle.active.textColor = Color.white;
            _dropdownStyle.focused.textColor = LoogaEditorStyle.TextColor;
            return _dropdownStyle;
        }

        private static Texture2D GetDropdownTexture(ref Texture2D texture, Color color)
        {
            if (texture != null)
                return texture;

            texture = new Texture2D(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private static void DrawDropdownArrow(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Rect snapped = LoogaEditorStyle.PixelSnap(rect);
            float size = LoogaEditorStyle.Pixels(7f);
            float centerX = LoogaEditorStyle.PixelSnapValue(snapped.xMax - LoogaEditorStyle.Pixels(10f));
            float centerY = LoogaEditorStyle.PixelSnapValue(snapped.center.y + LoogaEditorStyle.Pixels(1f));

            Vector3 left = new(centerX - size * 0.5f, centerY - size * 0.25f, 0f);
            Vector3 right = new(centerX + size * 0.5f, centerY - size * 0.25f, 0f);
            Vector3 bottom = new(centerX, centerY + size * 0.35f, 0f);

            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = LoogaEditorStyle.ArrowColor;
            Handles.DrawAAConvexPolygon(left, right, bottom);
            Handles.color = previous;
            Handles.EndGUI();
        }
        private static void DrawStatusIndicator(Rect rect, Color color)
        {
            Rect snapped = LoogaEditorStyle.PixelSnap(rect);
            Vector3 center = new(snapped.center.x, snapped.center.y, 0f);
            float radius = Mathf.Min(snapped.width, snapped.height) * 0.5f;

            Handles.BeginGUI();
            Color previous = Handles.color;
            Handles.color = color;
            Handles.DrawSolidDisc(center, Vector3.forward, radius);
            Handles.color = previous;
            Handles.EndGUI();
        }

        private static void DrawStatusActionOutline(Rect rect)
        {
            Color outline = EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.30f, 0.30f, 1f)
                : new Color(0.72f, 0.72f, 0.72f, 1f);
            Rect snapped = LoogaEditorStyle.PixelSnap(rect);
            float line = LoogaEditorStyle.Pixels(1f);

            EditorGUI.DrawRect(new Rect(snapped.xMin, snapped.yMin, snapped.width, line), outline);
            EditorGUI.DrawRect(new Rect(snapped.xMin, snapped.yMax - line, snapped.width, line), outline);
            EditorGUI.DrawRect(new Rect(snapped.xMin, snapped.yMin, line, snapped.height), outline);
            EditorGUI.DrawRect(new Rect(snapped.xMax - line, snapped.yMin, line, snapped.height), outline);
        }

        private static bool DrawTextActionButton(Rect rect, string label, string tooltip)
        {
            int controlId = GUIUtility.GetControlID("LoogaStatusBoxTextAction".GetHashCode(), FocusType.Passive, rect);
            Event current = Event.current;
            bool hovered = rect.Contains(current.mousePosition);
            bool pressed = GUIUtility.hotControl == controlId;

            if (hovered)
                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);

            if (current.type == EventType.Repaint)
            {
                Color color = !GUI.enabled
                    ? new Color(StatusBackgroundColor.r, StatusBackgroundColor.g, StatusBackgroundColor.b, 0.55f)
                    : pressed
                        ? StatusActionPressedColor
                        : hovered
                            ? StatusActionHoverColor
                            : StatusActionColor;

                Rect background = LoogaEditorStyle.PixelSnap(rect);
                EditorGUI.DrawRect(background, color);
                GUI.Label(background, new GUIContent(label, tooltip), GetStatusActionLabelStyle());
            }

            if (!GUI.enabled)
                return false;

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

        private static GUIStyle GetStatusMessageStyle()
        {
            GUIStyle style = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = true,
                clipping = TextClipping.Clip,
                fontSize = Mathf.Max(1, EditorStyles.label.fontSize - 1)
            };
            style.normal.textColor = LoogaEditorStyle.TextColor;
            return style;
        }

        private static GUIStyle GetStatusActionLabelStyle()
        {
            GUIStyle style = new(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 0, 1),
                fontSize = Mathf.Max(1, EditorStyles.label.fontSize - 1)
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

    }
}










