using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(StatusBoxAttribute))]
    public sealed class StatusBoxDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            StatusBoxAttribute statusAttribute = (StatusBoxAttribute)attribute;
            float helpHeight = GetStatusHeight(property, statusAttribute);
            if (helpHeight > 0f)
            {
                Rect helpRect = new(position.x, position.y, position.width, helpHeight);
                DrawStatusBox(helpRect, ResolveMessage(property, statusAttribute), statusAttribute.Type);
                position.y += helpHeight + EditorGUIUtility.standardVerticalSpacing;
                position.height -= helpHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true);
            float helpHeight = GetStatusHeight(property, (StatusBoxAttribute)attribute);
            return helpHeight <= 0f ? height : height + helpHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        internal static bool DrawStatusBox(
            Rect position,
            string message,
            LoogaStatusBoxType type,
            bool hasAction = false,
            string actionLabel = "",
            string actionTooltip = "Open")
        {
            Rect rect = LoogaEditorStyle.PixelSnap(position);
            EditorGUI.DrawRect(rect, LoogaEditorStyle.BoxColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, LoogaEditorStyle.AccentRailWidth, rect.height), GetAccentColor(type));

            const float Padding = 8f;
            const float ActionSize = 22f;
            float actionWidth = 0f;
            if (hasAction)
                actionWidth = string.IsNullOrWhiteSpace(actionLabel) ? ActionSize : Mathf.Min(150f, EditorStyles.miniButton.CalcSize(new GUIContent(actionLabel)).x + 16f);

            Rect labelRect = new(
                rect.x + LoogaEditorStyle.AccentRailWidth + Padding,
                rect.y,
                Mathf.Max(0f, rect.width - LoogaEditorStyle.AccentRailWidth - Padding * 2f - actionWidth - (hasAction ? Padding : 0f)),
                rect.height);

            GUI.Label(labelRect, new GUIContent(message), GetMessageStyle());

            if (!hasAction)
                return false;

            Rect actionRect = new(
                rect.xMax - Padding - actionWidth,
                rect.y + Mathf.Round((rect.height - ActionSize) * 0.5f),
                actionWidth,
                ActionSize);

            return string.IsNullOrWhiteSpace(actionLabel)
                ? DrawOpenActionButton(actionRect, actionTooltip)
                : DrawTextActionButton(actionRect, actionLabel, actionTooltip);
        }

        private static bool DrawTextActionButton(Rect rect, string label, string tooltip)
        {
            Event current = Event.current;
            bool hovered = rect.Contains(current.mousePosition);
            Color color = GUI.enabled
                ? hovered ? LoogaEditorStyle.HoverColor : LoogaEditorStyle.AlternateBoxColor
                : new Color(LoogaEditorStyle.BoxColor.r, LoogaEditorStyle.BoxColor.g, LoogaEditorStyle.BoxColor.b, 0.55f);

            if (current.type == EventType.Repaint)
                EditorGUI.DrawRect(LoogaEditorStyle.PixelSnap(rect), color);

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            return GUI.Button(rect, new GUIContent(label, tooltip), GetActionButtonStyle());
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
                Rect background = LoogaEditorStyle.PixelSnap(rect);
                Color color = pressed
                    ? LoogaEditorStyle.SelectionColor
                    : hovered
                        ? LoogaEditorStyle.HoverColor
                        : LoogaEditorStyle.AlternateBoxColor;
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
            Color lineColor = highlighted ? Color.white : LoogaEditorStyle.TextColor;
            Rect glyph = new(
                Mathf.Round(rect.x + 6f),
                Mathf.Round(rect.y + 6f),
                Mathf.Round(rect.width - 12f),
                Mathf.Round(rect.height - 12f));

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

        private static GUIStyle GetMessageStyle()
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

        private static GUIStyle GetActionButtonStyle()
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
            style.normal.textColor = LoogaEditorStyle.TextColor;
            style.hover.textColor = LoogaEditorStyle.TextColor;
            style.active.textColor = Color.white;
            return style;
        }

        private static Color GetAccentColor(LoogaStatusBoxType type)
        {
            return type switch
            {
                LoogaStatusBoxType.Warning => new Color(0.95f, 0.68f, 0.22f, 1f),
                LoogaStatusBoxType.Error => new Color(0.86f, 0.24f, 0.20f, 1f),
                _ => LoogaEditorStyle.ActionAccentColor
            };
        }
        internal static MessageType ToMessageType(LoogaStatusBoxType type)
        {
            return type switch
            {
                LoogaStatusBoxType.Warning => MessageType.Warning,
                LoogaStatusBoxType.Error => MessageType.Error,
                _ => MessageType.Info
            };
        }

        internal static bool ShouldShow(object target, StatusBoxAttribute statusAttribute)
        {
            if (statusAttribute == null)
                return false;

            if (string.IsNullOrWhiteSpace(statusAttribute.Condition))
                return true;

            bool condition = PropertyUtils.GetConditionValue(target, statusAttribute.Condition);
            return statusAttribute.Invert ? !condition : condition;
        }

        internal static string ResolveMessage(SerializedProperty property, StatusBoxAttribute statusAttribute)
        {
            return ResolveMessage(PropertyUtils.GetTargetObjectWithProperty(property), statusAttribute);
        }

        internal static string ResolveMessage(object target, StatusBoxAttribute statusAttribute)
        {
            if (statusAttribute == null)
                return string.Empty;

            if (!statusAttribute.UseMember || target == null || string.IsNullOrWhiteSpace(statusAttribute.Message))
                return statusAttribute.Message ?? string.Empty;

            object value = LoogaMemberValueUtility.GetValue(target, statusAttribute.Message);
            return value?.ToString() ?? string.Empty;
        }

        private static float GetStatusHeight(SerializedProperty property, StatusBoxAttribute statusAttribute)
        {
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            if (!ShouldShow(target, statusAttribute))
                return 0f;

            string message = ResolveMessage(target, statusAttribute);
            return string.IsNullOrWhiteSpace(message) ? 0f : GetStatusBoxHeight(message);
        }

        internal static float GetStatusBoxHeight(string message)
        {
            return EditorGUIUtility.singleLineHeight * 2.1f;
        }
    }
}
