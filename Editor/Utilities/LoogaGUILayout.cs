using System;
using LoogaSoft.Inspector.Runtime;
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
        private const float ResponsiveBreakpoint = 480f;
        private const float PropertySpacing = 2f;
        private const float InspectorHorizontalMargins = 38f;
        private const float NestedContentAllowance = 16f;
        private const float IndentWidth = 15f;
        private const float ToggleGlyphWidth = 18f;

        private static GUIStyle _wrappedLabelStyle;
        private static GUIStyle _wrappedToggleStyle;

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

        public static bool Notice(
            string message,
            LoogaNoticeType type = LoogaNoticeType.Info,
            bool hasAction = false,
            string actionLabel = "",
            string actionTooltip = "Open")
        {
            Rect rect = EditorGUILayout.GetControlRect(false, LoogaGUI.GetNoticeHeight(message));
            return LoogaGUI.Notice(rect, message, type, hasAction, actionLabel, actionTooltip);
        }

        /// <summary>
        /// Draws a serialized property using an inline row while it remains readable, then reflows it for
        /// narrow inspectors. Boolean properties become full-width checkbox rows; other controls stack
        /// beneath a naturally wrapping label.
        /// </summary>
        public static void PropertyField(
            SerializedProperty property,
            GUIContent label = null,
            bool includeChildren = true)
        {
            PropertyField(property, label, includeChildren, !CustomDrawerUtil.HasCustomDrawer(property));
        }

        internal static void PropertyField(
            SerializedProperty property,
            GUIContent label,
            bool includeChildren,
            bool allowResponsiveLayout)
        {
            label ??= PropertyUtils.GetLabel(property);

            if (!allowResponsiveLayout || !ShouldReflow(property, label))
            {
                EditorGUILayout.PropertyField(property, label, includeChildren);
                return;
            }

            if (property.propertyType == SerializedPropertyType.Boolean)
            {
                DrawWrappedBoolean(property, label);
                return;
            }

            DrawStackedProperty(property, label, includeChildren);
        }

        private static bool ShouldReflow(SerializedProperty property, GUIContent label)
        {
            if (property == null
                || label == null
                || string.IsNullOrWhiteSpace(label.text)
                || IsArrayElement(property))
            {
                return false;
            }

            float availableWidth = GetEstimatedContentWidth();
            if (availableWidth >= ResponsiveBreakpoint)
                return false;

            float inlineLabelWidth = GetInlineLabelWidth(availableWidth);
            float measuredLabelWidth = EditorStyles.label.CalcSize(label).x;
            float controlWidth = availableWidth - inlineLabelWidth - PropertySpacing;

            return measuredLabelWidth > inlineLabelWidth
                || controlWidth < GetMinimumControlWidth(property.propertyType);
        }

        private static void DrawWrappedBoolean(SerializedProperty property, GUIContent label)
        {
            float estimatedWidth = GetEstimatedContentWidth();
            float labelWidth = Mathf.Max(1f, estimatedWidth - ToggleGlyphWidth);
            float height = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                WrappedLabelStyle.CalcHeight(label, labelWidth));

            Rect rowRect = EditorGUILayout.GetControlRect(false, height);
            Rect contentRect = EditorGUI.IndentedRect(rowRect);
            int previousIndent = EditorGUI.indentLevel;
            bool previousMixedValue = EditorGUI.showMixedValue;

            EditorGUI.indentLevel = 0;
            EditorGUI.showMixedValue = property.hasMultipleDifferentValues;
            EditorGUI.BeginProperty(contentRect, label, property);
            EditorGUI.BeginChangeCheck();

            bool value = EditorGUI.ToggleLeft(contentRect, label, property.boolValue, WrappedToggleStyle);
            if (EditorGUI.EndChangeCheck())
                property.boolValue = value;

            EditorGUI.EndProperty();
            EditorGUI.showMixedValue = previousMixedValue;
            EditorGUI.indentLevel = previousIndent;
        }

        private static void DrawStackedProperty(
            SerializedProperty property,
            GUIContent label,
            bool includeChildren)
        {
            float estimatedWidth = GetEstimatedContentWidth();
            float labelHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight,
                WrappedLabelStyle.CalcHeight(label, estimatedWidth));
            float fieldHeight = EditorGUI.GetPropertyHeight(property, GUIContent.none, includeChildren);
            float totalHeight = labelHeight + PropertySpacing + fieldHeight;

            Rect rowRect = EditorGUILayout.GetControlRect(false, totalHeight);
            Rect contentRect = EditorGUI.IndentedRect(rowRect);
            Rect labelRect = new(contentRect.x, contentRect.y, contentRect.width, labelHeight);
            Rect fieldRect = new(
                contentRect.x,
                labelRect.yMax + PropertySpacing,
                contentRect.width,
                fieldHeight);

            int previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            EditorGUI.BeginProperty(labelRect, label, property);
            EditorGUI.LabelField(labelRect, label, WrappedLabelStyle);
            EditorGUI.EndProperty();
            EditorGUI.PropertyField(fieldRect, property, GUIContent.none, includeChildren);

            EditorGUI.indentLevel = previousIndent;
        }

        private static float GetEstimatedContentWidth()
        {
            return Mathf.Max(
                1f,
                EditorGUIUtility.currentViewWidth
                - InspectorHorizontalMargins
                - NestedContentAllowance
                - EditorGUI.indentLevel * IndentWidth);
        }

        private static float GetInlineLabelWidth(float availableWidth)
        {
            float configuredWidth = EditorGUIUtility.labelWidth;
            if (configuredWidth <= 0f)
                configuredWidth = 150f;

            return Mathf.Clamp(configuredWidth, 80f, availableWidth * 0.45f);
        }

        private static float GetMinimumControlWidth(SerializedPropertyType propertyType)
        {
            return propertyType switch
            {
                SerializedPropertyType.Boolean => 20f,
                SerializedPropertyType.Integer => 70f,
                SerializedPropertyType.Float => 70f,
                SerializedPropertyType.Enum => 110f,
                SerializedPropertyType.Color => 90f,
                SerializedPropertyType.Vector2 => 140f,
                SerializedPropertyType.Vector3 => 175f,
                SerializedPropertyType.Vector4 => 190f,
                SerializedPropertyType.Rect => 170f,
                SerializedPropertyType.Bounds => 180f,
                SerializedPropertyType.ObjectReference => 140f,
                _ => 120f
            };
        }

        private static bool IsArrayElement(SerializedProperty property)
        {
            return property.propertyPath.Contains(".Array.data[");
        }

        private static GUIStyle WrappedLabelStyle => _wrappedLabelStyle ??= new GUIStyle(EditorStyles.label)
        {
            wordWrap = true,
            clipping = TextClipping.Clip,
            fixedHeight = 0f
        };

        private static GUIStyle WrappedToggleStyle => _wrappedToggleStyle ??= new GUIStyle(EditorStyles.toggle)
        {
            wordWrap = true,
            clipping = TextClipping.Clip,
            fixedHeight = 0f
        };
    }
}
