using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class LoogaEditorFoldouts
    {
        public const float SmallPaddingX = 4f;
        public const float SmallPaddingY = 6f;

        private const string PropertyClipboardPrefix = "LOOGA_SERIALIZED_PROPERTY::";
        private const float SmallHoverExtraWidth = 4f;
        private const float SmallBoxGap = 4f;
        private const float LargeFoldoutGap = 2f;
        private const float HeaderLeftInset = 6f;
        private const float HeaderArrowSize = 7f;
        private const float HeaderTextArrowGap = 6f;

        private static GUIStyle _largeHeader;
        private static GUIStyle _smallHeader;
        private static GUIStyle _largeBox;
        private static GUIStyle _smallBox;
        private static EditorWindow _trackedMouseMoveWindow;
        private static bool _mouseMoveUpdateRegistered;
        private static int _containedFoldoutDepth;

        public static GUIStyle SmallBoxStyle
        {
            get
            {
                EnsureStyles();
                return _smallBox;
            }
        }

        public static void LoogaFoldoutLarge(string title, string prefKey, bool defaultShow, Action content)
        {
            EnsureStyles();

            bool show = EditorPrefs.GetBool(prefKey, defaultShow);

            EditorGUILayout.BeginVertical(_largeBox);
            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _largeHeader);
            Rect boxRect = ContentToBoxRect(baseRect, _largeBox);
            Rect headerRect = new(
                boxRect.x,
                boxRect.y,
                boxRect.width,
                baseRect.height + _largeBox.padding.top + 2f);
            Rect text = GetHeaderTextRect(headerRect, 1f, _largeBox);
            Rect arrow = GetHeaderArrowRect(headerRect, _largeBox);

            bool containsMouse = headerRect.Contains(Event.current.mousePosition);
            RequestMouseMoveRepaint(containsMouse);

            if (containsMouse)
                EditorGUI.DrawRect(headerRect, new Color(1f, 1f, 1f, 0.05f));

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

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(LargeFoldoutGap);
        }

        public static bool LoogaFoldoutSmall(Rect position, GUIContent label, bool expanded, out Rect contentRect, SerializedProperty property = null)
        {
            EnsureStyles();

            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;
            int oldIndent = EditorGUI.indentLevel;
            Rect indentedPosition = EditorGUI.IndentedRect(position);
            bool newExpanded;

            try
            {
                EditorGUI.indentLevel = 0;

                Rect boxRect = new(
                    indentedPosition.x,
                    indentedPosition.y,
                    indentedPosition.width,
                    indentedPosition.height + spacing - SmallBoxGap);
                GUI.Box(boxRect, GUIContent.none, _smallBox);

                Rect headerRect = new(
                    boxRect.x,
                    boxRect.y + 2f,
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

                newExpanded = LoogaFoldoutSmallHeader(headerRect, hoverRect, label, expanded, property);

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

            EditorGUILayout.BeginVertical(_smallBox);

            Rect baseRect = GUILayoutUtility.GetRect(GUIContent.none, _smallHeader);
            Rect boxRect = ContentToBoxRect(baseRect, _smallBox);
            Rect headerRect = new(
                boxRect.x,
                boxRect.y,
                boxRect.width,
                baseRect.height + _smallBox.padding.top + 1f);
            bool newExpanded = LoogaFoldoutSmallHeader(headerRect, headerRect, label, expanded, property, _smallBox);

            if (newExpanded)
            {
                EditorGUILayout.Space(2f);
                content?.Invoke();
                EditorGUILayout.Space(2f);
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(1f);

            return newExpanded;
        }

        public static bool LoogaFoldoutSmallHeader(Rect headerRect, GUIContent label, bool expanded, SerializedProperty property = null)
        {
            return LoogaFoldoutSmallHeader(headerRect, headerRect, label, expanded, property, _smallBox);
        }

        public static IDisposable ContainedFoldoutScope()
        {
            _containedFoldoutDepth++;
            return new ContainedFoldoutScopeInstance();
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

            boxStyle ??= _smallBox;
            Rect textRect = GetHeaderTextRect(headerRect, 1f, boxStyle);
            Rect arrowRect = GetHeaderArrowRect(headerRect, boxStyle);

            if (property != null)
                EditorGUI.BeginProperty(clickRect, label, property);

            Event current = Event.current;
            bool containsMouse = clickRect.Contains(current.mousePosition);
            RequestMouseMoveRepaint(containsMouse);

            if (containsMouse)
                EditorGUI.DrawRect(clickRect, new Color(1f, 1f, 1f, 0.05f));

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

        private static Rect GetHeaderTextRect(Rect headerRect, float yOffset, GUIStyle boxStyle)
        {
            float sideInset = GetHeaderSideInset(headerRect);
            float reservedRight = sideInset + HeaderArrowSize + HeaderTextArrowGap;

            return new Rect(
                headerRect.x + HeaderLeftInset,
                headerRect.y + yOffset,
                Mathf.Max(0f, headerRect.width - HeaderLeftInset - reservedRight),
                headerRect.height);
        }

        private static Rect GetHeaderArrowRect(Rect headerRect, GUIStyle boxStyle)
        {
            float sideInset = GetHeaderSideInset(headerRect);

            return new Rect(
                headerRect.xMax - sideInset - HeaderArrowSize,
                headerRect.y + sideInset,
                HeaderArrowSize,
                HeaderArrowSize);
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
            float half = HeaderArrowSize * 0.5f;
            Vector3[] points = expanded
                ? new[]
                {
                    new Vector3(center.x - half, center.y - half * 0.45f, 0f),
                    new Vector3(center.x + half, center.y - half * 0.45f, 0f),
                    new Vector3(center.x, center.y + half * 0.55f, 0f)
                }
                : new[]
                {
                    new Vector3(center.x - half * 0.35f, center.y - half, 0f),
                    new Vector3(center.x - half * 0.35f, center.y + half, 0f),
                    new Vector3(center.x + half * 0.55f, center.y, 0f)
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
                padding = new RectOffset(0, 0, 0, 3)
            };

            _largeBox = new GUIStyle("HelpBox")
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 4, 4)
            };

            _smallBox = new GUIStyle("HelpBox")
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(8, 8, 3, 1)
            };
        }
    }
}
