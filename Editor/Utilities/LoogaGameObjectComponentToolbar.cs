using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Adds a Looga-styled component clipboard immediately above the Transform component header.
    /// The clipboard is editor-session only and copies serialized component data, excluding Transform.
    /// </summary>
    [CustomEditor(typeof(Transform))]
    [CanEditMultipleObjects]
    internal sealed class LoogaGameObjectComponentToolbar : UnityEditor.Editor
    {
        private const float ToolbarHeight = 26f;
        private const float ButtonHeight = 18f;
        private const float HorizontalPadding = 4f;
        private const float ButtonGap = 4f;
        private const float ButtonWidth = 126f;
        private const float CountLabelWidth = 120f;

        private static readonly List<CopiedComponent> CopiedComponents = new();
        private static GUIStyle _toolbarStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _labelStyle;
        private static Texture2D _toolbarTexture;
        private static Texture2D _buttonTexture;
        private static Texture2D _buttonHoverTexture;
        private static Texture2D _buttonActiveTexture;
        private static string _sourceName;

        private UnityEditor.Editor _transformEditor;

        private static bool HasClipboard => CopiedComponents.Count > 0;

        private void OnEnable()
        {
            CreateTransformEditor();
        }

        private void OnDisable()
        {
            if (_transformEditor != null)
            {
                DestroyImmediate(_transformEditor);
                _transformEditor = null;
            }
        }

        protected override void OnHeaderGUI()
        {
            DrawComponentToolbar();

            if (_transformEditor != null)
            {
                _transformEditor.DrawHeader();
                return;
            }

            base.OnHeaderGUI();
        }

        public override void OnInspectorGUI()
        {
            if (_transformEditor != null)
            {
                _transformEditor.OnInspectorGUI();
                return;
            }

            DrawDefaultInspector();
        }

        public override bool RequiresConstantRepaint()
        {
            return _transformEditor != null && _transformEditor.RequiresConstantRepaint();
        }

        private void CreateTransformEditor()
        {
            Type transformInspectorType = Type.GetType("UnityEditor.TransformInspector, UnityEditor");
            if (transformInspectorType == null)
                return;

            _transformEditor = CreateEditor(targets, transformInspectorType);
        }

        private void DrawComponentToolbar()
        {
            if (target is not Transform transform)
                return;

            EnsureStyles();

            Rect rect = GUILayoutUtility.GetRect(0f, ToolbarHeight, GUILayout.ExpandWidth(true));
            rect = LoogaEditorStyle.PixelSnap(new Rect(
                rect.x + HorizontalPadding,
                rect.y,
                rect.width - HorizontalPadding * 2f,
                rect.height));

            if (Event.current.type == EventType.Repaint)
                _toolbarStyle.Draw(rect, GUIContent.none, false, false, false, false);

            Rect buttonRect = GetCenteredRect(rect, rect.x + HorizontalPadding, ButtonWidth, ButtonHeight);
            if (GUI.Button(buttonRect, "Copy Components", _buttonStyle))
                CopyComponents(transform.gameObject);

            float nextX = buttonRect.xMax + ButtonGap;
            if (HasClipboard)
            {
                Rect pasteRect = GetCenteredRect(rect, nextX, ButtonWidth, ButtonHeight);
                if (GUI.Button(pasteRect, "Paste Components", _buttonStyle))
                    PasteComponents(targets);

                nextX = pasteRect.xMax + ButtonGap;
                Rect countRect = GetCenteredRect(rect, nextX, CountLabelWidth, ButtonHeight);
                GUI.Label(countRect, $"{CopiedComponents.Count} copied", _labelStyle);
            }
        }

        private static Rect GetCenteredRect(Rect container, float x, float width, float height)
        {
            float y = container.y + (container.height - height) * 0.5f;
            return LoogaEditorStyle.PixelSnap(new Rect(x, y, width, height));
        }

        private static void CopyComponents(GameObject source)
        {
            CopiedComponents.Clear();
            _sourceName = source != null ? source.name : string.Empty;

            if (source == null)
                return;

            Component[] components = source.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || component is Transform)
                    continue;

                Type type = component.GetType();
                try
                {
                    string typeName = type.AssemblyQualifiedName ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(typeName))
                        continue;

                    CopiedComponents.Add(new CopiedComponent(typeName, EditorJsonUtility.ToJson(component)));
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Looga Inspector] Could not copy component '{type.Name}' from '{source.name}'. {exception.Message}", source);
                }
            }
        }

        private static void PasteComponents(UnityEngine.Object[] transformTargets)
        {
            if (!HasClipboard || transformTargets == null)
                return;

            int pastedCount = 0;
            for (int i = 0; i < transformTargets.Length; i++)
            {
                if (transformTargets[i] is not Transform transform)
                    continue;

                GameObject targetGameObject = transform.gameObject;
                for (int componentIndex = 0; componentIndex < CopiedComponents.Count; componentIndex++)
                {
                    CopiedComponent copied = CopiedComponents[componentIndex];
                    Type type = Type.GetType(copied.typeName);
                    if (type == null || !typeof(Component).IsAssignableFrom(type) || typeof(Transform).IsAssignableFrom(type))
                        continue;

                    try
                    {
                        Component component = Undo.AddComponent(targetGameObject, type);
                        EditorJsonUtility.FromJsonOverwrite(copied.json, component);
                        pastedCount++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[Looga Inspector] Could not paste component '{type.Name}' onto '{targetGameObject.name}'. {exception.Message}", targetGameObject);
                    }
                }

                EditorUtility.SetDirty(targetGameObject);
            }

            if (pastedCount > 0)
            {
                string label = string.IsNullOrWhiteSpace(_sourceName) ? "copied GameObject" : _sourceName;
                Debug.Log($"[Looga Inspector] Pasted {pastedCount} copied component(s) from '{label}'.");
            }
        }

        private static void EnsureStyles()
        {
            _toolbarTexture ??= CreateTexture(LoogaEditorStyle.BoxColor);
            _buttonTexture ??= CreateTexture(LoogaEditorStyle.ListRowColor);
            _buttonHoverTexture ??= CreateTexture(LoogaEditorStyle.ListHoverColor);
            _buttonActiveTexture ??= CreateTexture(LoogaEditorStyle.SelectionColor);

            _toolbarStyle ??= new GUIStyle
            {
                normal = { background = _toolbarTexture },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 2, 2)
            };

            _buttonStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                normal = { background = _buttonTexture, textColor = LoogaEditorStyle.TextColor },
                hover = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                active = { background = _buttonActiveTexture, textColor = Color.white },
                focused = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                padding = new RectOffset(8, 8, 0, 0)
            };

            _labelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = LoogaEditorStyle.TextColor }
            };
        }

        private static Texture2D CreateTexture(Color color)
        {
            Texture2D texture = new(1, 1)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private readonly struct CopiedComponent
        {
            public readonly string typeName;
            public readonly string json;

            public CopiedComponent(string typeName, string json)
            {
                this.typeName = typeName;
                this.json = json;
            }
        }
    }
}