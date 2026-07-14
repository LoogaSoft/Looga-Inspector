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
        private const float NoticePadding = 2f;
        private const float NoticeActionSize = 18f;
        private const string DefaultNoticeActionLabel = "Open";
        private const float NoticeIndicatorSize = 5f;

        private static Color NoticeBackgroundColor => EditorGUIUtility.isProSkin
            ? new Color(0.225f, 0.225f, 0.225f, 1f)
            : new Color(0.79f, 0.79f, 0.79f, 1f);

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
            return EditorGUI.Popup(fieldRect, GUIContent.none, selectedIndex, displayedOptions);
        }

        public static int MaskField(Rect position, GUIContent label, int mask, string[] displayedOptions)
        {
            Rect fieldRect = GetLabeledFieldRect(position, label);
            return EditorGUI.MaskField(fieldRect, GUIContent.none, mask, displayedOptions);
        }


        public static bool Notice(
            Rect position,
            string message,
            LoogaNoticeType type = LoogaNoticeType.Info,
            bool hasAction = false,
            string actionLabel = "",
            string actionTooltip = "Open")
        {
            Rect rect = LoogaEditorStyle.PixelSnap(position);
            EditorGUI.DrawRect(rect, NoticeBackgroundColor);

            Rect indicatorRect = new(
                rect.x + NoticePadding + 1f,
                rect.y + Mathf.Round((rect.height - NoticeIndicatorSize) * 0.5f),
                NoticeIndicatorSize,
                NoticeIndicatorSize);
            DrawNoticeIndicator(indicatorRect, GetNoticeAccentColor(type));

            if (hasAction && string.IsNullOrWhiteSpace(actionLabel))
                actionLabel = DefaultNoticeActionLabel;

            float actionWidth = 0f;
            if (hasAction)
                actionWidth = Mathf.Min(150f, EditorStyles.miniButton.CalcSize(new GUIContent(actionLabel)).x + 14f);

            float labelX = indicatorRect.xMax + NoticePadding + 3f;
            Rect labelRect = new(
                labelX,
                rect.y,
                Mathf.Max(0f, rect.xMax - labelX - NoticePadding - actionWidth - (hasAction ? NoticePadding : 0f)),
                rect.height);

            GUI.Label(labelRect, new GUIContent(message), GetNoticeMessageStyle());

            if (!hasAction)
                return false;

            Rect actionRect = new(
                rect.xMax - NoticePadding - actionWidth,
                rect.y + Mathf.Round((rect.height - NoticeActionSize) * 0.5f),
                actionWidth,
                NoticeActionSize);

            return GUI.Button(actionRect, new GUIContent(actionLabel, actionTooltip), EditorStyles.miniButton);
        }

        public static float GetNoticeHeight(string message)
        {
            return string.IsNullOrWhiteSpace(message)
                ? 0f
                : Mathf.Ceil(EditorGUIUtility.singleLineHeight + NoticePadding * 2f);
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

        private static void DrawNoticeIndicator(Rect rect, Color color)
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



        private static GUIStyle GetNoticeMessageStyle()
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

        private static Color GetNoticeAccentColor(LoogaNoticeType type)
        {
            return type switch
            {
                LoogaNoticeType.Warning => new Color(0.95f, 0.68f, 0.22f, 1f),
                LoogaNoticeType.Error => new Color(0.86f, 0.24f, 0.20f, 1f),
                _ => LoogaEditorStyle.ActionAccentColor
            };
        }

    }
}










