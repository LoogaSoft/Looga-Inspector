using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Adds a small Looga-styled component clipboard beneath Unity's built-in GameObject header.
    /// The clipboard is editor-session only and copies serialized component data, excluding Transform.
    /// </summary>
    [InitializeOnLoad]
    internal static class LoogaGameObjectComponentToolbar
    {
        private const float ToolbarHeight = 28f;
        private const float HorizontalPadding = 4f;
        private const float VerticalPadding = 3f;
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

        static LoogaGameObjectComponentToolbar()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawToolbar;
        }

        private static bool HasClipboard => CopiedComponents.Count > 0;

        private static void DrawToolbar(UnityEditor.Editor editor)
        {
            if (editor == null || editor.target is not GameObject gameObject)
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

            Rect contentRect = LoogaEditorStyle.PixelSnap(new Rect(
                rect.x + HorizontalPadding,
                rect.y + VerticalPadding,
                rect.width - HorizontalPadding * 2f,
                rect.height - VerticalPadding * 2f));

            Rect copyRect = new(contentRect.x, contentRect.y, ButtonWidth, contentRect.height);
            if (GUI.Button(copyRect, "Copy Components", _buttonStyle))
                CopyComponents(gameObject);

            float nextX = copyRect.xMax + ButtonGap;
            if (HasClipboard)
            {
                Rect pasteRect = new(nextX, contentRect.y, ButtonWidth, contentRect.height);
                if (GUI.Button(pasteRect, "Paste Components", _buttonStyle))
                    PasteComponents(editor.targets);

                nextX = pasteRect.xMax + ButtonGap;
                Rect countRect = new(nextX, contentRect.y, CountLabelWidth, contentRect.height);
                GUI.Label(countRect, $"{CopiedComponents.Count} copied", _labelStyle);
            }
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

        private static void PasteComponents(UnityEngine.Object[] targets)
        {
            if (!HasClipboard || targets == null)
                return;

            int pastedCount = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not GameObject target)
                    continue;

                for (int componentIndex = 0; componentIndex < CopiedComponents.Count; componentIndex++)
                {
                    CopiedComponent copied = CopiedComponents[componentIndex];
                    Type type = Type.GetType(copied.typeName);
                    if (type == null || !typeof(Component).IsAssignableFrom(type) || typeof(Transform).IsAssignableFrom(type))
                        continue;

                    try
                    {
                        Component component = Undo.AddComponent(target, type);
                        EditorJsonUtility.FromJsonOverwrite(copied.json, component);
                        pastedCount++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[Looga Inspector] Could not paste component '{type.Name}' onto '{target.name}'. {exception.Message}", target);
                    }
                }

                EditorUtility.SetDirty(target);
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