using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class LoogaEditorFoldouts
    {
        public const float SmallPaddingX = 10f;
        public const float SmallPaddingY = 6f;

        private const string PropertyClipboardPrefix = "LOOGA_SERIALIZED_PROPERTY::";
        private const float SmallHoverExtraWidth = 4f;
        private const float SmallBoxGap = 6f;
        private const float SmallLayoutHoverBottomBleed = 4f;
        private const float LargeFoldoutGap = 2f;
        private const float BoxHorizontalInset = 3f;
        private const float HeaderLeftInset = 6f;
        private const float HeaderArrowSize = 10.5f;
        private const float HeaderArrowLeftNudge = 0f;
        private const float HeaderTextArrowGap = 6f;
        private const int AccentRailWidth = 4;

        private static GUIStyle _largeHeader;
        private static GUIStyle _smallHeader;
        private static GUIStyle _largeBox;
        private static GUIStyle _largeFoldoutBox;
        private static GUIStyle _nestedLargeBox;
        private static GUIStyle _nestedLargeFoldoutBox;
        private static GUIStyle _smallBox;
        private static GUIStyle _smallFoldoutBox;
        private static GUIStyle _nestedSmallBox;
        private static GUIStyle _nestedSmallFoldoutBox;
        private static GUIStyle _smallLayoutBox;
        private static GUIStyle _smallLayoutFoldoutBox;
        private static GUIStyle _nestedSmallLayoutBox;
        private static GUIStyle _nestedSmallLayoutFoldoutBox;
        private static Texture2D _flatBoxTexture;
        private static Texture2D _nestedFlatBoxTexture;
        private static EditorWindow _trackedMouseMoveWindow;
        private static bool _mouseMoveUpdateRegistered;
        private static int _boxDepth;
        private static int _containedFoldoutDepth;

        public static GUIStyle SmallBoxStyle
        {
            get
            {
                EnsureStyles();
                return GetSmallBoxStyle();
            }
        }

        public static GUIStyle SmallFoldoutBoxStyle
        {
            get
            {
                EnsureStyles();
                return GetSmallFoldoutBoxStyle();
            }
        }

        public static void LoogaFoldoutLarge(string title, string prefKey, bool defaultShow, Action content)
        {
            EnsureStyles();

            bool show = EditorPrefs.GetBool(prefKey, defaultShow);
            GUIStyle boxStyle = GetLargeFoldoutBoxStyle();

            EditorGUILayout.BeginVertical(boxStyle);
            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _largeHeader);
            using (BoxDepthScope())
            {
                Rect boxRect = ContentToBoxRect(baseRect, boxStyle);
                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    baseRect.height + boxStyle.padding.top + 2f);
                Rect text = GetHeaderTextRect(headerRect, 1f, boxStyle);
                Rect arrow = GetHeaderArrowRect(headerRect, boxStyle);

                Rect hoverRect = show ? headerRect : boxRect;
                bool containsMouse = hoverRect.Contains(Event.current.mousePosition);
                RequestMouseMoveRepaint(containsMouse);

                if (containsMouse)
                    DrawHoverRect(hoverRect);

                GUI.Label(text, title, _largeHeader);

                bool newShow = show;
                DrawFoldoutArrow(arrow, show);
                if (Event.current.type == EventType.MouseDown && headerRect.Contains(Event.current.mousePosition) && Event.current.button == 0)
                {
                    newShow = !show;
                    Event.current.Use();
                }

                if (newShow != show)
                {
                    EditorPrefs.SetBool(prefKey, newShow);
                    show = newShow;
                }

                if (show)
                {
                    EditorGUILayout.Space(2);
                    content?.Invoke();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(LargeFoldoutGap);
        }

        public static void LoogaBoxLarge(string title, Action content)
        {
            EnsureStyles();

            GUIStyle boxStyle = GetLargeBoxStyle();
            EditorGUILayout.BeginVertical(boxStyle);
            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _largeHeader);
            using (BoxDepthScope())
            {
                Rect boxRect = ContentToBoxRect(baseRect, boxStyle);
                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    baseRect.height + boxStyle.padding.top + 2f);
                Rect text = GetStaticHeaderTextRect(headerRect, 1f);

                GUI.Label(text, title, _largeHeader);
                EditorGUILayout.Space(2);
                content?.Invoke();
                EditorGUILayout.Space(2);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(LargeFoldoutGap);
        }

        public static bool LoogaFoldoutLarge(Rect position, GUIContent label, bool expanded, out Rect contentRect, SerializedProperty property = null)
        {
            EnsureStyles();

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            int oldIndent = EditorGUI.indentLevel;
            Rect indentedPosition = ShrinkBoxRect(EditorGUI.IndentedRect(position));
            bool newExpanded;
            GUIStyle boxStyle = GetLargeFoldoutBoxStyle();

            try
            {
                EditorGUI.indentLevel = 0;

                GUI.Box(indentedPosition, GUIContent.none, boxStyle);

                Rect headerRect = new(
                    indentedPosition.x,
                    indentedPosition.y + 2f,
                    indentedPosition.width,
                    lineHeight + boxStyle.padding.top + 2f);
                Rect hoverRect = expanded ? headerRect : indentedPosition;
                Rect textRect = GetHeaderTextRect(headerRect, 1f, boxStyle);
                Rect arrowRect = GetHeaderArrowRect(headerRect, boxStyle);

                if (property != null)
                    EditorGUI.BeginProperty(hoverRect, label, property);

                Event current = Event.current;
                bool containsMouse = hoverRect.Contains(current.mousePosition);
                RequestMouseMoveRepaint(containsMouse);

                if (containsMouse)
                    DrawHoverRect(hoverRect);

                GUI.Label(textRect, label, _largeHeader);

                newExpanded = expanded;
                DrawFoldoutArrow(arrowRect, expanded);
                if (property != null && current.type == EventType.ContextClick && containsMouse)
                {
                    ShowPropertyContextMenu(property);
                    current.Use();
                }
                else if (current.type == EventType.MouseDown && containsMouse && current.button == 0)
                {
                    newExpanded = !expanded;
                    current.Use();
                }

                if (property != null)
                    EditorGUI.EndProperty();

                contentRect = new Rect(
                    indentedPosition.x + boxStyle.padding.left,
                    headerRect.yMax + spacing,
                    indentedPosition.width - boxStyle.padding.horizontal,
                    indentedPosition.height - headerRect.height - boxStyle.padding.vertical - spacing);
            }
            finally
            {
                EditorGUI.indentLevel = oldIndent;
            }

            return newExpanded;
        }

        public static bool LoogaFoldoutSmall(Rect position, GUIContent label, bool expanded, out Rect contentRect, SerializedProperty property = null)
        {
            EnsureStyles();

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            int oldIndent = EditorGUI.indentLevel;
            Rect indentedPosition = ShrinkBoxRect(EditorGUI.IndentedRect(position));
            bool newExpanded;
            GUIStyle boxStyle = GetSmallFoldoutBoxStyle();

            try
            {
                EditorGUI.indentLevel = 0;

                Rect boxRect = new(
                    indentedPosition.x,
                    indentedPosition.y,
                    indentedPosition.width,
                    indentedPosition.height + spacing - SmallBoxGap);
                GUI.Box(boxRect, GUIContent.none, boxStyle);

                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    lineHeight + 2f);
                bool allowHoverOverflow = Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout;
                Rect hoverRect = allowHoverOverflow
                    ? new Rect(
                        headerRect.x - SmallHoverExtraWidth * 0.5f,
                        headerRect.y,
                        headerRect.width + SmallHoverExtraWidth,
                        headerRect.height)
                    : headerRect;

                Rect clickRect = expanded ? hoverRect : boxRect;
                newExpanded = LoogaFoldoutSmallHeader(headerRect, clickRect, label, expanded, property, boxStyle);

                contentRect = new Rect(
                    boxRect.x + SmallPaddingX,
                    headerRect.yMax + spacing,
                    boxRect.width - SmallPaddingX * 2f,
                    boxRect.height - headerRect.height - SmallPaddingY);
            }
            finally
            {
                EditorGUI.indentLevel = oldIndent;
            }

            return newExpanded;
        }

        public static bool LoogaFoldoutSmall(GUIContent label, bool expanded, Action content, SerializedProperty property = null)
        {
            EnsureStyles();
            
            EditorGUILayout.Space(1f);

            GUIStyle boxStyle = GetSmallLayoutFoldoutBoxStyle();
            EditorGUILayout.BeginVertical(boxStyle);

            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _smallHeader);
            bool newExpanded;
            using (BoxDepthScope())
            {
                Rect boxRect = ContentToBoxRect(baseRect, boxStyle);
                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    baseRect.height + boxStyle.padding.top + 2f);
                Rect clickRect = expanded ? headerRect : ExpandRectBottom(boxRect, SmallLayoutHoverBottomBleed);
                newExpanded = LoogaFoldoutSmallHeader(headerRect, clickRect, label, expanded, property, boxStyle);

                if (newExpanded)
                {
                    EditorGUILayout.Space(2f);
                    content?.Invoke();
                    EditorGUILayout.Space(2f);
                }
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(1f);

            return newExpanded;
        }

        public static void LoogaBoxSmall(GUIContent label, Action content)
        {
            EnsureStyles();

            EditorGUILayout.Space(1f);
            GUIStyle boxStyle = GetSmallLayoutBoxStyle();
            EditorGUILayout.BeginVertical(boxStyle);

            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _smallHeader);
            using (BoxDepthScope())
            {
                Rect boxRect = ContentToBoxRect(baseRect, boxStyle);
                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    baseRect.height + boxStyle.padding.top + 2f);
                Rect textRect = GetStaticHeaderTextRect(headerRect, 1f);

                GUI.Label(textRect, label, _smallHeader);
                EditorGUILayout.Space(2f);
                content?.Invoke();
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1f);
        }

        public static bool LoogaFoldoutSmallHeader(Rect headerRect, GUIContent label, bool expanded, SerializedProperty property = null)
        {
            return LoogaFoldoutSmallHeader(headerRect, headerRect, label, expanded, property, GetSmallBoxStyle());
        }

        public static IDisposable ContainedFoldoutScope()
        {
            _containedFoldoutDepth++;
            return new ContainedFoldoutScopeInstance();
        }

        private static IDisposable BoxDepthScope()
        {
            _boxDepth++;
            return new BoxDepthScopeInstance();
        }

        private static bool LoogaFoldoutSmallHeader(
            Rect headerRect,
            Rect clickRect,
            GUIContent label,
            bool expanded,
            SerializedProperty property = null,
            GUIStyle boxStyle = null)
        {
            EnsureStyles();

            boxStyle ??= GetSmallBoxStyle();
            Rect textRect = GetHeaderTextRect(headerRect, 1f, boxStyle);
            Rect arrowRect = GetHeaderArrowRect(headerRect, boxStyle);

            if (property != null)
                EditorGUI.BeginProperty(clickRect, label, property);

            Event current = Event.current;
            bool containsMouse = clickRect.Contains(current.mousePosition);
            RequestMouseMoveRepaint(containsMouse);

            if (containsMouse)
                DrawHoverRect(clickRect);

            GUI.Label(textRect, label, _smallHeader);

            bool newExpanded = expanded;
            DrawFoldoutArrow(arrowRect, expanded);

            if (property != null && current.type == EventType.ContextClick && containsMouse)
            {
                ShowPropertyContextMenu(property);
                current.Use();
            }
            else if (current.type == EventType.MouseDown && containsMouse && current.button == 0)
            {
                newExpanded = !expanded;
                current.Use();
            }

            if (property != null)
                EditorGUI.EndProperty();

            return newExpanded;
        }

        public static void LoogaToggleFoldoutLarge(string title, SerializedProperty toggleProperty, string prefKey, Action content)
        {
            EnsureStyles();

            bool enabled = toggleProperty != null && toggleProperty.propertyType == SerializedPropertyType.Boolean && toggleProperty.boolValue;
            bool show = enabled && EditorPrefs.GetBool(prefKey, false);
            GUIStyle boxStyle = GetLargeFoldoutBoxStyle();

            EditorGUILayout.BeginVertical(boxStyle);
            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _largeHeader);
            using (BoxDepthScope())
            {
                Rect boxRect = ContentToBoxRect(baseRect, boxStyle);
                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    baseRect.height + boxStyle.padding.top + 2f);
                Rect toggleRect = GetHeaderToggleRect(headerRect);
                Rect arrowRect = GetHeaderArrowRectAfter(headerRect, toggleRect);
                Rect textRect = GetHeaderTextRectAfter(headerRect, arrowRect, 1f);

                Event current = Event.current;
                Rect hoverRect = show ? headerRect : boxRect;
                bool containsMouse = hoverRect.Contains(current.mousePosition);
                RequestMouseMoveRepaint(containsMouse);

                if (containsMouse)
                    DrawHoverRect(hoverRect);

                EditorGUI.BeginChangeCheck();
                bool newEnabled = EditorGUI.Toggle(toggleRect, enabled);
                if (EditorGUI.EndChangeCheck() && toggleProperty != null)
                {
                    toggleProperty.boolValue = newEnabled;
                    enabled = newEnabled;
                    show = false;
                    EditorPrefs.SetBool(prefKey, false);
                }

                GUI.Label(textRect, title, _largeHeader);

                if (enabled)
                {
                    DrawFoldoutArrow(arrowRect, show);
                    if (current.type == EventType.MouseDown
                        && hoverRect.Contains(current.mousePosition)
                        && !toggleRect.Contains(current.mousePosition)
                        && current.button == 0)
                    {
                        show = !show;
                        EditorPrefs.SetBool(prefKey, show);
                        current.Use();
                    }
                }

                if (enabled && show)
                {
                    EditorGUILayout.Space(2f);
                    content?.Invoke();
                    EditorGUILayout.Space(2f);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(LargeFoldoutGap);
        }

        public static bool LoogaToggleFoldoutSmall(GUIContent label, SerializedProperty toggleProperty, bool expanded, Action content, SerializedProperty property = null)
        {
            EnsureStyles();

            bool enabled = toggleProperty != null && toggleProperty.propertyType == SerializedPropertyType.Boolean && toggleProperty.boolValue;
            bool show = enabled && expanded;

            EditorGUILayout.Space(1f);
            GUIStyle boxStyle = GetSmallLayoutFoldoutBoxStyle();
            EditorGUILayout.BeginVertical(boxStyle);

            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _smallHeader);
            using (BoxDepthScope())
            {
                Rect boxRect = ContentToBoxRect(baseRect, boxStyle);
                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y,
                    boxRect.width,
                    baseRect.height + boxStyle.padding.top + 2f);
                Rect toggleRect = GetHeaderToggleRect(headerRect);
                Rect arrowRect = GetHeaderArrowRectAfter(headerRect, toggleRect);
                Rect textRect = GetHeaderTextRectAfter(headerRect, arrowRect, 1f);

                Event current = Event.current;
                Rect hoverRect = show ? headerRect : ExpandRectBottom(boxRect, SmallLayoutHoverBottomBleed);
                bool containsMouse = hoverRect.Contains(current.mousePosition);
                RequestMouseMoveRepaint(containsMouse);

                if (containsMouse)
                    DrawHoverRect(hoverRect);

                if (property != null)
                    EditorGUI.BeginProperty(headerRect, label, property);

                EditorGUI.BeginChangeCheck();
                bool newEnabled = EditorGUI.Toggle(toggleRect, enabled);
                if (EditorGUI.EndChangeCheck() && toggleProperty != null)
                {
                    toggleProperty.boolValue = newEnabled;
                    enabled = newEnabled;
                    show = false;
                }

                GUI.Label(textRect, label, _smallHeader);

                if (enabled)
                {
                    DrawFoldoutArrow(arrowRect, show);
                    if (current.type == EventType.MouseDown
                        && hoverRect.Contains(current.mousePosition)
                        && !toggleRect.Contains(current.mousePosition)
                        && current.button == 0)
                    {
                        show = !show;
                        current.Use();
                    }
                }

                if (property != null)
                    EditorGUI.EndProperty();

                if (enabled && show)
                {
                    EditorGUILayout.Space(2f);
                    content?.Invoke();
                    EditorGUILayout.Space(2f);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(1f);

            return enabled && show;
        }
        private static void RequestMouseMoveRepaint(bool containsMouse)
        {
            EditorWindow window = EditorWindow.mouseOverWindow;
            if (window == null)
                return;

            window.wantsMouseMove = true;
            TrackMouseMoveWindow(window);

            if (containsMouse && Event.current.type != EventType.Layout)
            {
                window.Repaint();
            }
        }

        private static Rect ContentToBoxRect(Rect contentRect, GUIStyle boxStyle)
        {
            RectOffset padding = boxStyle.padding;
            return new Rect(
                contentRect.x - padding.left,
                contentRect.y - padding.top,
                contentRect.width + padding.horizontal,
                contentRect.height + padding.vertical);
        }

        private static Rect ExpandRectBottom(Rect rect, float amount)
        {
            rect.height += amount;
            return rect;
        }

        private static Rect GetHeaderTextRect(Rect headerRect, float yOffset, GUIStyle boxStyle)
        {
            Rect arrowRect = GetHeaderArrowRect(headerRect, boxStyle);
            float x = arrowRect.xMax + HeaderTextArrowGap;
            float rightInset = GetHeaderSideInset(headerRect);

            return new Rect(
                x,
                GetHeaderTextY(headerRect, yOffset),
                Mathf.Max(0f, headerRect.xMax - rightInset - x),
                EditorGUIUtility.singleLineHeight);
        }

        private static Rect GetStaticHeaderTextRect(Rect headerRect, float yOffset)
        {
            return new Rect(
                headerRect.x + HeaderLeftInset + AccentRailWidth,
                GetHeaderTextY(headerRect, yOffset),
                Mathf.Max(0f, headerRect.width - HeaderLeftInset * 2f - AccentRailWidth),
                EditorGUIUtility.singleLineHeight);
        }

        private static Rect GetHeaderToggleRect(Rect headerRect)
        {
            float size = EditorGUIUtility.singleLineHeight - 2f;
            return new Rect(
                headerRect.x + HeaderLeftInset + AccentRailWidth,
                CenterVertically(headerRect, size).y,
                size,
                size);
        }

        private static Rect GetHeaderArrowRectAfter(Rect headerRect, Rect previousRect)
        {
            return new Rect(
                previousRect.xMax + HeaderTextArrowGap,
                CenterVertically(headerRect, HeaderArrowSize).y,
                HeaderArrowSize,
                HeaderArrowSize);
        }

        private static Rect GetHeaderTextRectAfter(Rect headerRect, Rect previousRect, float yOffset)
        {
            float x = previousRect.xMax + HeaderTextArrowGap;
            float rightInset = GetHeaderSideInset(headerRect);
            return new Rect(
                x,
                GetHeaderTextY(headerRect, yOffset),
                Mathf.Max(0f, headerRect.xMax - rightInset - x),
                EditorGUIUtility.singleLineHeight);
        }

        private static Rect GetHeaderArrowRect(Rect headerRect, GUIStyle boxStyle)
        {
            return new Rect(
                headerRect.x + HeaderLeftInset + AccentRailWidth + HeaderArrowLeftNudge,
                CenterVertically(headerRect, HeaderArrowSize).y,
                HeaderArrowSize,
                HeaderArrowSize);
        }

        private static float GetHeaderTextY(Rect headerRect, float yOffset)
        {
            return CenterVertically(headerRect, EditorGUIUtility.singleLineHeight).y + yOffset;
        }

        private static Rect CenterVertically(Rect container, float height)
        {
            return new Rect(
                container.x,
                SnapToPixel(container.y + (container.height - height) * 0.5f),
                container.width,
                height);
        }

        private static float SnapToPixel(float value)
        {
            float pixelsPerPoint = EditorGUIUtility.pixelsPerPoint;
            return Mathf.Floor(value * pixelsPerPoint + 0.5f) / pixelsPerPoint;
        }

        private static float GetHeaderSideInset(Rect headerRect)
        {
            return Mathf.Max(2f, (headerRect.height - HeaderArrowSize) * 0.5f);
        }

        private static void DrawFoldoutArrow(Rect arrowRect, bool expanded)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color previousColor = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.68f, 0.68f, 0.68f, 1f)
                : new Color(0.28f, 0.28f, 0.28f, 1f);

            Vector2 center = arrowRect.center;
            float radius = HeaderArrowSize * 0.5f;
            float verticalRadius = radius * Mathf.Sqrt(3f) * 0.5f;
            Vector3[] points = expanded
                ? new[]
                {
                    new Vector3(center.x - radius, center.y - verticalRadius * 0.75f, 0f),
                    new Vector3(center.x + radius, center.y - verticalRadius * 0.75f, 0f),
                    new Vector3(center.x, center.y + verticalRadius * 0.75f, 0f)
                }
                : new[]
                {
                    new Vector3(center.x - verticalRadius * 0.5f, center.y - radius, 0f),
                    new Vector3(center.x - verticalRadius * 0.5f, center.y + radius, 0f),
                    new Vector3(center.x + verticalRadius, center.y, 0f)
                };

            Handles.BeginGUI();
            Handles.DrawAAConvexPolygon(points);
            Handles.EndGUI();
            Handles.color = previousColor;
        }

        private sealed class ContainedFoldoutScopeInstance : IDisposable
        {
            public void Dispose()
            {
                _containedFoldoutDepth = Mathf.Max(0, _containedFoldoutDepth - 1);
            }
        }

        private sealed class BoxDepthScopeInstance : IDisposable
        {
            public void Dispose()
            {
                _boxDepth = Mathf.Max(0, _boxDepth - 1);
            }
        }

        private static void TrackMouseMoveWindow(EditorWindow window)
        {
            _trackedMouseMoveWindow = window;

            if (_mouseMoveUpdateRegistered)
                return;

            EditorApplication.update += RepaintTrackedMouseMoveWindow;
            _mouseMoveUpdateRegistered = true;
        }

        private static void RepaintTrackedMouseMoveWindow()
        {
            if (_trackedMouseMoveWindow == null)
            {
                EditorApplication.update -= RepaintTrackedMouseMoveWindow;
                _mouseMoveUpdateRegistered = false;
                return;
            }

            if (EditorWindow.mouseOverWindow != _trackedMouseMoveWindow)
                return;

            _trackedMouseMoveWindow.wantsMouseMove = true;
            _trackedMouseMoveWindow.Repaint();
        }

        private static void ShowPropertyContextMenu(SerializedProperty property)
        {
            GenericMenu menu = new();

            menu.AddItem(new GUIContent("Copy Property Path"), false, () =>
            {
                EditorGUIUtility.systemCopyBuffer = property.propertyPath;
            });
            menu.AddDisabledItem(new GUIContent("Search Same Property Value"));
            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Copy"), false, () => CopyProperty(property));

            if (CanPasteProperty(property))
                menu.AddItem(new GUIContent("Paste"), false, () => PasteProperty(property));
            else
                menu.AddDisabledItem(new GUIContent("Paste"));

            menu.ShowAsContext();
        }

        private static void CopyProperty(SerializedProperty property)
        {
            if (!TryGetPropertyPayload(property, out PropertyClipboardPayload payload))
                return;

            EditorGUIUtility.systemCopyBuffer = PropertyClipboardPrefix + JsonUtility.ToJson(payload);
        }

        private static bool CanPasteProperty(SerializedProperty property)
        {
            return TryReadClipboardPayload(out PropertyClipboardPayload payload)
                && IsPasteCompatible(property, payload);
        }

        private static void PasteProperty(SerializedProperty property)
        {
            if (!TryReadClipboardPayload(out PropertyClipboardPayload payload))
                return;

            if (!IsPasteCompatible(property, payload))
                return;

            property.serializedObject.Update();

            for (int i = 0; i < payload.properties.Count; i++)
            {
                SerializedPropertyValue source = payload.properties[i];
                SerializedProperty target = property.FindPropertyRelative(source.relativePath);

                if (target == null || (int)target.propertyType != source.propertyType)
                    continue;

                ApplyPropertyValue(target, source);
            }

            property.serializedObject.ApplyModifiedProperties();
            GUI.changed = true;
        }

        private static bool TryGetPropertyPayload(SerializedProperty property, out PropertyClipboardPayload payload)
        {
            payload = null;

            if (!TryGetTargetType(property, out Type targetType))
                return false;

            List<SerializedPropertyValue> values = new();
            CollectPropertyValues(property, values);

            if (values.Count == 0)
                return false;

            payload = new PropertyClipboardPayload
            {
                typeName = targetType.AssemblyQualifiedName,
                properties = values
            };
            return true;
        }

        private static bool TryReadClipboardPayload(out PropertyClipboardPayload payload)
        {
            payload = null;
            string copyBuffer = EditorGUIUtility.systemCopyBuffer;

            if (string.IsNullOrEmpty(copyBuffer) || !copyBuffer.StartsWith(PropertyClipboardPrefix, StringComparison.Ordinal))
                return false;

            string json = copyBuffer.Substring(PropertyClipboardPrefix.Length);
            payload = JsonUtility.FromJson<PropertyClipboardPayload>(json);

            return payload != null
                && payload.properties != null;
        }

        private static bool TryGetTargetType(SerializedProperty property, out Type targetType)
        {
            targetType = CustomDrawerUtil.GetTargetType(property);
            return targetType != null && property.propertyType == SerializedPropertyType.Generic;
        }

        private static bool IsPasteCompatible(SerializedProperty property, PropertyClipboardPayload payload)
        {
            if (property == null || payload?.properties == null || payload.properties.Count == 0)
                return false;

            if (TryGetTargetType(property, out Type targetType)
                && !string.IsNullOrEmpty(payload.typeName)
                && payload.typeName == targetType.AssemblyQualifiedName)
            {
                return true;
            }

            for (int i = 0; i < payload.properties.Count; i++)
            {
                SerializedPropertyValue source = payload.properties[i];
                SerializedProperty target = property.FindPropertyRelative(source.relativePath);

                if (target == null || (int)target.propertyType != source.propertyType)
                    return false;
            }

            return true;
        }

        private static void CollectPropertyValues(SerializedProperty root, List<SerializedPropertyValue> values)
        {
            SerializedProperty iterator = root.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int rootDepth = root.depth;
            string rootPath = root.propertyPath;

            if (!iterator.NextVisible(true))
                return;

            do
            {
                if (iterator.depth <= rootDepth || SerializedProperty.EqualContents(iterator, endProperty))
                    break;

                if (iterator.propertyType == SerializedPropertyType.Generic)
                    continue;

                SerializedPropertyValue value = CreatePropertyValue(rootPath, iterator);
                if (value != null)
                    values.Add(value);
            } while (iterator.NextVisible(true));
        }

        private static SerializedPropertyValue CreatePropertyValue(string rootPath, SerializedProperty property)
        {
            SerializedPropertyValue value = new()
            {
                relativePath = GetRelativePath(rootPath, property.propertyPath),
                propertyType = (int)property.propertyType
            };

            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                    value.longValue = property.longValue;
                    return value;
                case SerializedPropertyType.Boolean:
                    value.boolValue = property.boolValue;
                    return value;
                case SerializedPropertyType.Float:
                    value.doubleValue = property.doubleValue;
                    return value;
                case SerializedPropertyType.String:
                    value.stringValue = property.stringValue;
                    return value;
                case SerializedPropertyType.Color:
                    value.colorValue = property.colorValue;
                    return value;
                case SerializedPropertyType.ObjectReference:
                    value.objectReferenceValue = property.objectReferenceValue;
                    return value;
                case SerializedPropertyType.Vector2:
                    value.vector2Value = property.vector2Value;
                    return value;
                case SerializedPropertyType.Vector3:
                    value.vector3Value = property.vector3Value;
                    return value;
                case SerializedPropertyType.Vector4:
                    value.vector4Value = property.vector4Value;
                    return value;
                case SerializedPropertyType.Rect:
                    value.rectValue = property.rectValue;
                    return value;
                case SerializedPropertyType.AnimationCurve:
                    value.animationCurveValue = new AnimationCurve(property.animationCurveValue.keys);
                    value.animationCurveValue.preWrapMode = property.animationCurveValue.preWrapMode;
                    value.animationCurveValue.postWrapMode = property.animationCurveValue.postWrapMode;
                    return value;
                case SerializedPropertyType.Bounds:
                    value.boundsValue = property.boundsValue;
                    return value;
                case SerializedPropertyType.Quaternion:
                    value.quaternionValue = property.quaternionValue;
                    return value;
                case SerializedPropertyType.Vector2Int:
                    value.vector2IntValue = property.vector2IntValue;
                    return value;
                case SerializedPropertyType.Vector3Int:
                    value.vector3IntValue = property.vector3IntValue;
                    return value;
                case SerializedPropertyType.RectInt:
                    value.rectIntValue = property.rectIntValue;
                    return value;
                case SerializedPropertyType.BoundsInt:
                    value.boundsIntValue = property.boundsIntValue;
                    return value;
                default:
                    return null;
            }
        }

        private static void ApplyPropertyValue(SerializedProperty property, SerializedPropertyValue value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.LayerMask:
                case SerializedPropertyType.Enum:
                    property.longValue = value.longValue;
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = value.boolValue;
                    break;
                case SerializedPropertyType.Float:
                    property.doubleValue = value.doubleValue;
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value.stringValue;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = value.colorValue;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value.objectReferenceValue;
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = value.vector2Value;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = value.vector3Value;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = value.vector4Value;
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = value.rectValue;
                    break;
                case SerializedPropertyType.AnimationCurve:
                    if (value.animationCurveValue != null)
                    {
                        AnimationCurve curve = new(value.animationCurveValue.keys)
                        {
                            preWrapMode = value.animationCurveValue.preWrapMode,
                            postWrapMode = value.animationCurveValue.postWrapMode
                        };
                        property.animationCurveValue = curve;
                    }
                    else
                    {
                        property.animationCurveValue = null;
                    }
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = value.boundsValue;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = value.quaternionValue;
                    break;
                case SerializedPropertyType.Vector2Int:
                    property.vector2IntValue = value.vector2IntValue;
                    break;
                case SerializedPropertyType.Vector3Int:
                    property.vector3IntValue = value.vector3IntValue;
                    break;
                case SerializedPropertyType.RectInt:
                    property.rectIntValue = value.rectIntValue;
                    break;
                case SerializedPropertyType.BoundsInt:
                    property.boundsIntValue = value.boundsIntValue;
                    break;
            }
        }

        private static string GetRelativePath(string rootPath, string propertyPath)
        {
            if (propertyPath.Length <= rootPath.Length)
                return string.Empty;

            return propertyPath.Substring(rootPath.Length + 1);
        }

        [Serializable]
        private sealed class PropertyClipboardPayload
        {
            public string typeName;
            public List<SerializedPropertyValue> properties = new();
        }

        [Serializable]
        private sealed class SerializedPropertyValue
        {
            public string relativePath;
            public int propertyType;
            public long longValue;
            public bool boolValue;
            public double doubleValue;
            public string stringValue;
            public Color colorValue;
            public UnityEngine.Object objectReferenceValue;
            public Vector2 vector2Value;
            public Vector3 vector3Value;
            public Vector4 vector4Value;
            public Rect rectValue;
            public AnimationCurve animationCurveValue;
            public Bounds boundsValue;
            public Quaternion quaternionValue;
            public Vector2Int vector2IntValue;
            public Vector3Int vector3IntValue;
            public RectInt rectIntValue;
            public BoundsInt boundsIntValue;
        }

        private static void EnsureStyles()
        {
            if (_largeHeader != null)
                return;

            _largeHeader = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(0, 0, 0, 4)
            };

            _smallHeader = new GUIStyle(EditorStyles.label)
            {
                //fontSize = 13,
                padding = new RectOffset(0, 0, 0, 2)
            };

            _flatBoxTexture = CreateFlatTexture(GetFlatBoxColor());
            _nestedFlatBoxTexture = CreateFlatTexture(GetNestedFlatBoxColor());

            _largeBox = CreateFlatBoxStyle(new RectOffset(8, 8, 4, 2), false, false);
            _largeFoldoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 4, 2), true, false);
            _nestedLargeBox = CreateFlatBoxStyle(new RectOffset(8, 8, 4, 2), false, true);
            _nestedLargeFoldoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 4, 2), true, true);
            _smallBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, 0), false, false);
            _smallFoldoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, 0), true, false);
            _nestedSmallBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, 0), false, true);
            _nestedSmallFoldoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, 0), true, true);
            _smallLayoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, -2), false, false);
            _smallLayoutFoldoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, -2), true, false);
            _nestedSmallLayoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, -2), false, true);
            _nestedSmallLayoutFoldoutBox = CreateFlatBoxStyle(new RectOffset(8, 8, 3, -2), true, true);
        }

        public static void DrawHoverRect(Rect rect)
        {
            Rect hoverRect = rect;
            hoverRect.xMin += Mathf.Max(0f, AccentRailWidth - 1f);
            EditorGUI.DrawRect(hoverRect, GetFlatHoverColor());
            DrawAccentRail(rect);
        }

        private static void DrawAccentRail(Rect rect)
        {
            Rect railRect = new(rect.x, rect.y, AccentRailWidth, rect.height);
            EditorGUI.DrawRect(railRect, GetAccentRailColor());
        }

        private static Color GetFlatHoverColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.275f, 0.275f, 0.275f, 1f)
                : new Color(0.68f, 0.68f, 0.68f, 1f);
        }

        private static GUIStyle GetLargeBoxStyle()
        {
            return UseNestedStyle() ? _nestedLargeBox : _largeBox;
        }

        private static GUIStyle GetLargeFoldoutBoxStyle()
        {
            return UseNestedStyle() ? _nestedLargeFoldoutBox : _largeFoldoutBox;
        }

        private static GUIStyle GetSmallBoxStyle()
        {
            return UseNestedStyle() ? _nestedSmallBox : _smallBox;
        }

        private static GUIStyle GetSmallFoldoutBoxStyle()
        {
            return UseNestedStyle() ? _nestedSmallFoldoutBox : _smallFoldoutBox;
        }

        private static GUIStyle GetSmallLayoutBoxStyle()
        {
            return UseNestedStyle() ? _nestedSmallLayoutBox : _smallLayoutBox;
        }

        private static GUIStyle GetSmallLayoutFoldoutBoxStyle()
        {
            return UseNestedStyle() ? _nestedSmallLayoutFoldoutBox : _smallLayoutFoldoutBox;
        }

        private static bool UseNestedStyle()
        {
            return ((_boxDepth + _containedFoldoutDepth) & 1) == 1;
        }

        private static GUIStyle CreateFlatBoxStyle(RectOffset padding, bool includeAccentRail, bool nested)
        {
            Color boxColor = nested ? GetNestedFlatBoxColor() : GetFlatBoxColor();
            Texture2D texture = includeAccentRail
                ? CreateFlatTexture(boxColor, GetAccentRailColor())
                : nested ? _nestedFlatBoxTexture : _flatBoxTexture;

            GUIStyle style = new(EditorStyles.label)
            {
                margin = new RectOffset((int)BoxHorizontalInset, (int)BoxHorizontalInset, 0, 0),
                padding = padding,
                border = includeAccentRail ? new RectOffset(AccentRailWidth, 0, 0, 0) : new RectOffset(0, 0, 0, 0),
                overflow = new RectOffset(0, 0, 0, 0)
            };

            style.normal.background = texture;
            style.hover.background = texture;
            style.active.background = texture;
            style.focused.background = texture;
            return style;
        }

        private static Texture2D CreateFlatTexture(Color color)
        {
            return CreateFlatTexture(color, color);
        }

        private static Texture2D CreateFlatTexture(Color color, Color accentColor)
        {
            const int width = 8;
            Texture2D texture = new(width, 1)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, 0, x < AccentRailWidth ? accentColor : color);
            }

            texture.Apply();
            return texture;
        }

        private static Color GetAccentRailColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.38f, 0.38f, 0.38f, 1f)
                : new Color(0.52f, 0.52f, 0.52f, 1f);
        }

        private static Color GetFlatBoxColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.188f, 0.188f, 0.188f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);
        }

        private static Color GetNestedFlatBoxColor()
        {
            Color baseColor = GetFlatBoxColor();
            float offset = EditorGUIUtility.isProSkin ? -0.026f : -0.045f;
            return new Color(
                Mathf.Clamp01(baseColor.r + offset),
                Mathf.Clamp01(baseColor.g + offset),
                Mathf.Clamp01(baseColor.b + offset),
                baseColor.a);
        }

        private static Rect ShrinkBoxRect(Rect rect)
        {
            return new Rect(
                rect.x + BoxHorizontalInset,
                rect.y,
                Mathf.Max(0f, rect.width - BoxHorizontalInset * 2f),
                rect.height);
        }
    }
}




















