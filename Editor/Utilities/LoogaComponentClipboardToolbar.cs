using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Adds a Looga-styled component clipboard above the first component in GameObject inspectors.
    /// This uses Unity's inspector UIElements tree so the toolbar appears below header/version-control rows.
    /// </summary>
    [InitializeOnLoad]
    internal static class LoogaComponentClipboardToolbar
    {
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string ToolbarName = "Looga Component Clipboard Toolbar";
        private const float ToolbarHeight = 26f;
        private const float HorizontalPadding = 0f;
        private const float ButtonGap = 2f;
        private const float ButtonWidth = 28f;
        private const float CountLabelWidth = 120f;
        private const string CopyIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/copy.png";
        private const string PasteIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/clipboard-paste.png";
        private const string PasteValuesIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/paste-values.png";

        private static readonly List<InspectorToolbarContainer> Containers = new();
        private static readonly System.Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static GUIStyle _toolbarStyle;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _labelStyle;
        private static Texture2D _toolbarTexture;
        private static Texture2D _buttonTexture;
        private static Texture2D _buttonHoverTexture;
        private static Texture2D _buttonActiveTexture;
        private static Texture2D _copyIcon;
        private static Texture2D _pasteIcon;
        private static Texture2D _pasteValuesIcon;

        static LoogaComponentClipboardToolbar()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            EditorApplication.update -= RefreshInspectorWindows;
            EditorApplication.update += RefreshInspectorWindows;
            Selection.selectionChanged -= MarkInspectorsDirty;
            Selection.selectionChanged += MarkInspectorsDirty;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }

        private static void Dispose()
        {
            EditorApplication.update -= RefreshInspectorWindows;
            Selection.selectionChanged -= MarkInspectorsDirty;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;

            for (int i = Containers.Count - 1; i >= 0; i--)
                Containers[i].RemoveToolbar();

            Containers.Clear();
        }

        private static void RefreshInspectorWindows()
        {
            if (AllInspectorsField == null || InspectorWindowType == null)
                return;

            if (AllInspectorsField.GetValue(null) is not IList windows)
                return;

            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i] is not EditorWindow inspectorWindow || HasContainer(inspectorWindow))
                    continue;

                Containers.Add(new InspectorToolbarContainer(inspectorWindow));
            }

            for (int i = Containers.Count - 1; i >= 0; i--)
            {
                if (!Containers[i].IsValid)
                {
                    Containers[i].RemoveToolbar();
                    Containers.RemoveAt(i);
                    continue;
                }

                Containers[i].Update();
            }
        }

        private static bool HasContainer(EditorWindow window)
        {
            for (int i = 0; i < Containers.Count; i++)
            {
                if (Containers[i].Window == window)
                    return true;
            }

            return false;
        }

        private static void MarkInspectorsDirty()
        {
            for (int i = 0; i < Containers.Count; i++)
                Containers[i].NeedsSelectionRefresh = true;
        }

        private static void DrawToolbar(GameObject gameObject, Object[] pasteTargets)
        {
            if (gameObject == null)
                return;

            EnsureStyles();

            Rect rect = LoogaEditorStyle.PixelSnap(GUILayoutUtility.GetRect(0f, ToolbarHeight, GUILayout.ExpandWidth(true)));
            RequestMouseMoveRepaint(rect);

            if (Event.current.type == EventType.Repaint)
                _toolbarStyle.Draw(rect, GUIContent.none, false, false, false, false);

            Rect contentRect = LoogaEditorStyle.PixelSnap(new Rect(
                rect.x + HorizontalPadding,
                rect.y,
                rect.width - HorizontalPadding * 2f,
                rect.height));
            Rect buttonRect = LoogaEditorStyle.PixelSnap(new Rect(contentRect.x, contentRect.y, ButtonWidth, contentRect.height));
            if (IconButton(buttonRect, _copyIcon, "Copy components"))
                LoogaComponentClipboard.CopyComponents(gameObject);

            float nextX = buttonRect.xMax + ButtonGap;
            if (LoogaComponentClipboard.HasClipboard)
            {
                Rect pasteRect = LoogaEditorStyle.PixelSnap(new Rect(nextX, contentRect.y, ButtonWidth, contentRect.height));
                if (IconButton(pasteRect, _pasteIcon, "Paste components"))
                    LoogaComponentClipboard.PasteComponents(pasteTargets);

                nextX = pasteRect.xMax + ButtonGap;
                Rect pasteValuesRect = LoogaEditorStyle.PixelSnap(new Rect(nextX, contentRect.y, ButtonWidth, contentRect.height));
                if (IconButton(pasteValuesRect, _pasteValuesIcon, "Paste values into matching components"))
                    LoogaComponentClipboard.PasteValuesIntoMatchingComponents(pasteTargets);

                nextX = pasteValuesRect.xMax + ButtonGap;
                Rect countRect = LoogaEditorStyle.PixelSnap(new Rect(nextX, contentRect.y, CountLabelWidth, contentRect.height));
                GUI.Label(countRect, $"{LoogaComponentClipboard.CopiedCount} copied", _labelStyle);
            }
        }

        private static bool IconButton(Rect rect, Texture2D icon, string tooltip)
        {
            GUIContent content = new(icon, tooltip);
            return GUI.Button(rect, content, _buttonStyle);
        }

        private static void RequestMouseMoveRepaint(Rect rect)
        {
            Event current = Event.current;
            if (!rect.Contains(current.mousePosition))
                return;

            EditorWindow window = EditorWindow.mouseOverWindow;
            if (window != null)
                window.wantsMouseMove = true;

            if (current.type == EventType.MouseMove)
            {
                window?.Repaint();
                HandleUtility.Repaint();
            }
        }

        private static GameObject ResolveGameObject(Object target)
        {
            return target switch
            {
                GameObject gameObject => gameObject,
                Component component => component.gameObject,
                _ => null
            };
        }

        private static int ToolbarIndex(Object target)
        {
            if (target != null && AssetDatabase.Contains(target))
            {
                PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType(target);
                if (prefabType is PrefabAssetType.Regular or PrefabAssetType.Variant)
                    return 2;
            }

            return 1;
        }

        private static void EnsureStyles()
        {
            _toolbarTexture ??= CreateTexture(LoogaEditorStyle.BoxColor);
            _buttonTexture ??= CreateTexture(LoogaEditorStyle.ListRowColor);
            _buttonHoverTexture ??= CreateTexture(LoogaEditorStyle.ListHoverColor);
            _buttonActiveTexture ??= CreateTexture(LoogaEditorStyle.SelectionColor);
            _copyIcon ??= AssetDatabase.LoadAssetAtPath<Texture2D>(CopyIconPath);
            _pasteIcon ??= AssetDatabase.LoadAssetAtPath<Texture2D>(PasteIconPath);
            _pasteValuesIcon ??= AssetDatabase.LoadAssetAtPath<Texture2D>(PasteValuesIconPath);

            _toolbarStyle ??= new GUIStyle
            {
                normal = { background = _toolbarTexture },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _buttonStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                fontSize = 12,
                normal = { background = _buttonTexture, textColor = LoogaEditorStyle.TextColor },
                hover = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                active = { background = _buttonActiveTexture, textColor = Color.white },
                focused = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _labelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
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

        private sealed class InspectorToolbarContainer
        {
            private readonly PropertyInfo _lockedProperty;
            private VisualElement _editorList;
            private IMGUIContainer _toolbar;
            private Object _inspectingObject;
            private bool _wasLocked;

            public InspectorToolbarContainer(EditorWindow window)
            {
                Window = window;
                _lockedProperty = window.GetType().GetProperty("isLocked", BindingFlags.Public | BindingFlags.Instance);
                _wasLocked = IsLocked;
                NeedsSelectionRefresh = true;
            }

            public EditorWindow Window { get; }
            public bool NeedsSelectionRefresh { get; set; }
            public bool IsValid => Window != null;

            private bool IsLocked => _lockedProperty != null && Window != null && (bool)_lockedProperty.GetValue(Window);

            public void Update()
            {
                if (Window == null)
                    return;

                RefreshSelectionIfNeeded();
                GameObject gameObject = ResolveGameObject(_inspectingObject);
                if (gameObject == null)
                {
                    RemoveToolbar();
                    return;
                }

                _editorList ??= Window.rootVisualElement.Q(null, InspectorListClassName);
                if (_editorList == null)
                    return;

                RemoveDuplicateToolbar();
                if (_toolbar == null)
                    CreateToolbar();

                if (_toolbar.parent != _editorList)
                {
                    int index = Mathf.Clamp(ToolbarIndex(_inspectingObject), 0, _editorList.childCount);
                    _editorList.Insert(index, _toolbar);
                }
            }

            public void RemoveToolbar()
            {
                _toolbar?.RemoveFromHierarchy();
            }

            private void RefreshSelectionIfNeeded()
            {
                bool locked = IsLocked;
                if (!locked)
                    _inspectingObject = Selection.activeObject;
                else if (!_wasLocked || NeedsSelectionRefresh)
                    _inspectingObject ??= Selection.activeObject;

                _wasLocked = locked;
                NeedsSelectionRefresh = false;
            }

            private void CreateToolbar()
            {
                _toolbar = new IMGUIContainer(() => DrawToolbar(ResolveGameObject(_inspectingObject), new[] { _inspectingObject }))
                {
                    name = ToolbarName
                };
                _toolbar.style.height = ToolbarHeight;
                _toolbar.style.minHeight = ToolbarHeight;
                _toolbar.style.marginLeft = 0f;
                _toolbar.style.marginRight = 0f;
                _toolbar.style.paddingLeft = 0f;
                _toolbar.style.paddingRight = 0f;
                _toolbar.style.paddingTop = 0f;
                _toolbar.style.paddingBottom = 0f;
                _toolbar.style.flexShrink = 0f;
                _toolbar.style.marginTop = 0f;
                _toolbar.style.marginBottom = 0f;
            }

            private void RemoveDuplicateToolbar()
            {
                if (_editorList == null)
                    return;

                List<VisualElement> duplicates = new();
                _editorList.Query<VisualElement>(ToolbarName).ForEach(element =>
                {
                    if (element != _toolbar)
                        duplicates.Add(element);
                });

                for (int i = 0; i < duplicates.Count; i++)
                    duplicates[i].RemoveFromHierarchy();
            }
        }
    }
}
