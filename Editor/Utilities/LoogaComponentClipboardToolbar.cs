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
    /// Adds a Looga-styled IMGUI component clipboard above the first component in GameObject inspectors.
    /// UI Toolkit is only used to place the IMGUI block below Unity's GameObject header/version-control rows.
    /// </summary>
    [InitializeOnLoad]
    internal static class LoogaComponentClipboardToolbar
    {
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string ToolbarName = "Looga Component Clipboard Toolbar";
        private const int AllComponentsButtonId = -1;
        private const float ToolbarHeight = 26f;
        private const float ToolbarPadding = 1f;
        private const float ButtonGap = 1f;
        private const float ClipboardButtonWidth = 28f;
        private const float ClipboardButtonHeight = 24f;
        private const float ComponentButtonHeight = 23f;
        private const float ComponentButtonHorizontalPadding = 6f;
        private const float ComponentIconSize = 14f;
        private const float CountLabelWidth = 120f;
        private const string CopyIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/copy.png";
        private const string PasteIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/clipboard-paste.png";
        private const string PasteValuesIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/paste-values.png";

        private static readonly List<InspectorToolbarContainer> Containers = new();
        private static readonly System.Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static GUIStyle _toolbarStyle;
        private static GUIStyle _clipboardButtonStyle;
        private static GUIStyle _componentButtonStyle;
        private static GUIStyle _componentButtonSelectedStyle;
        private static GUIStyle _countLabelStyle;
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

        private static void DrawToolbar(GameObject gameObject, Object[] pasteTargets, HashSet<int> selectedComponentIds)
        {
            if (gameObject == null)
                return;

            EnsureStyles();

            Rect rect = LoogaEditorStyle.PixelSnap(GUILayoutUtility.GetRect(0f, CalculateToolbarHeight(gameObject, EditorGUIUtility.currentViewWidth), GUILayout.ExpandWidth(true)));
            RequestMouseMoveRepaint(rect);

            if (Event.current.type == EventType.Repaint)
                _toolbarStyle.Draw(rect, GUIContent.none, false, false, false, false);

            Rect contentRect = LoogaEditorStyle.PixelSnap(new Rect(
                rect.x + ToolbarPadding,
                rect.y + ToolbarPadding,
                rect.width - ToolbarPadding * 2f,
                rect.height - ToolbarPadding * 2f));

            DrawClipboardRow(gameObject, pasteTargets, selectedComponentIds, contentRect);
            DrawComponentRows(gameObject, selectedComponentIds, new Rect(
                contentRect.x,
                contentRect.y + ToolbarHeight,
                contentRect.width,
                Mathf.Max(0f, contentRect.height - ToolbarHeight)));
        }

        private static void DrawClipboardRow(GameObject gameObject, Object[] pasteTargets, HashSet<int> selectedComponentIds, Rect contentRect)
        {
            Rect buttonRect = LoogaEditorStyle.PixelSnap(new Rect(contentRect.x, contentRect.y, ClipboardButtonWidth, ClipboardButtonHeight));
            if (IconButton(buttonRect, _copyIcon, "Copy selected components"))
                LoogaComponentClipboard.CopyComponents(gameObject, selectedComponentIds);

            float nextX = buttonRect.xMax + ButtonGap;
            if (!LoogaComponentClipboard.HasClipboard)
                return;

            Rect pasteRect = LoogaEditorStyle.PixelSnap(new Rect(nextX, contentRect.y, ClipboardButtonWidth, ClipboardButtonHeight));
            if (IconButton(pasteRect, _pasteIcon, "Paste components"))
                LoogaComponentClipboard.PasteComponents(pasteTargets);

            nextX = pasteRect.xMax + ButtonGap;
            Rect pasteValuesRect = LoogaEditorStyle.PixelSnap(new Rect(nextX, contentRect.y, ClipboardButtonWidth, ClipboardButtonHeight));
            if (IconButton(pasteValuesRect, _pasteValuesIcon, "Paste values into matching components"))
                LoogaComponentClipboard.PasteValuesIntoMatchingComponents(pasteTargets);

            nextX = pasteValuesRect.xMax + ButtonGap + ToolbarPadding;
            Rect countRect = LoogaEditorStyle.PixelSnap(new Rect(nextX, contentRect.y, CountLabelWidth, ClipboardButtonHeight));
            GUI.Label(countRect, $"{LoogaComponentClipboard.CopiedCount} copied", _countLabelStyle);
        }

        private static void DrawComponentRows(GameObject gameObject, HashSet<int> selectedComponentIds, Rect rect)
        {
            if (rect.height <= 0f)
                return;

            List<ComponentButtonInfo> buttons = BuildComponentButtons(gameObject);
            List<ComponentButtonInfo> row = new();
            float rowWidth = 0f;
            float y = rect.y;

            for (int i = 0; i < buttons.Count; i++)
            {
                ComponentButtonInfo button = buttons[i];
                float nextWidth = row.Count == 0 ? button.MinWidth : rowWidth + ButtonGap + button.MinWidth;
                if (row.Count > 0 && nextWidth > rect.width)
                {
                    DrawComponentButtonRow(row, selectedComponentIds, rect.x, y, rect.width);
                    row.Clear();
                    rowWidth = 0f;
                    y += ComponentButtonHeight + ButtonGap;
                }

                row.Add(button);
                rowWidth = row.Count == 1 ? button.MinWidth : rowWidth + ButtonGap + button.MinWidth;
            }

            if (row.Count > 0)
                DrawComponentButtonRow(row, selectedComponentIds, rect.x, y, rect.width);
        }

        private static void DrawComponentButtonRow(List<ComponentButtonInfo> row, HashSet<int> selectedComponentIds, float x, float y, float width)
        {
            float minWidth = 0f;
            for (int i = 0; i < row.Count; i++)
                minWidth += row[i].MinWidth;

            minWidth += ButtonGap * Mathf.Max(0, row.Count - 1);
            float extraPerButton = row.Count > 0 ? Mathf.Max(0f, width - minWidth) / row.Count : 0f;
            float nextX = x;

            for (int i = 0; i < row.Count; i++)
            {
                ComponentButtonInfo button = row[i];
                float buttonWidth = button.MinWidth + extraPerButton;
                Rect rect = LoogaEditorStyle.PixelSnap(new Rect(nextX, y, buttonWidth, ComponentButtonHeight));
                bool selected = button.Id == AllComponentsButtonId ? selectedComponentIds.Count == 0 : selectedComponentIds.Contains(button.Id);
                GUIStyle style = selected ? _componentButtonSelectedStyle : _componentButtonStyle;

                if (GUI.Button(rect, button.Content, style))
                {
                    if (button.Id == AllComponentsButtonId)
                    {
                        selectedComponentIds.Clear();
                    }
                    else if (!selectedComponentIds.Add(button.Id))
                    {
                        selectedComponentIds.Remove(button.Id);
                    }

                    GUI.changed = true;
                }

                nextX = rect.xMax + ButtonGap;
            }
        }

        private static bool IconButton(Rect rect, Texture2D icon, string tooltip)
        {
            GUIContent content = new(icon, tooltip);
            return GUI.Button(rect, content, _clipboardButtonStyle);
        }

        private static List<ComponentButtonInfo> BuildComponentButtons(GameObject gameObject)
        {
            List<ComponentButtonInfo> buttons = new()
            {
                new ComponentButtonInfo(AllComponentsButtonId, new GUIContent("All", "Show and copy all components"), ComponentButtonWidth("All", false))
            };

            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (!ShouldShowComponentButton(component))
                    continue;

                string label = ComponentName(component);
                Texture image = component != null ? EditorGUIUtility.ObjectContent(component, component.GetType()).image : null;
                buttons.Add(new ComponentButtonInfo(
                    component != null ? component.GetInstanceID() : 0,
                    new GUIContent(label, image, label),
                    ComponentButtonWidth(label, image != null)));
            }

            return buttons;
        }

        private static float ComponentButtonWidth(string label, bool hasIcon)
        {
            float textWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(label)).x;
            float iconWidth = hasIcon ? ComponentIconSize + ButtonGap : 0f;
            return Mathf.Ceil(textWidth + iconWidth + ComponentButtonHorizontalPadding * 2f);
        }

        private static float CalculateToolbarHeight(GameObject gameObject, float availableWidth)
        {
            float rowWidth = Mathf.Max(1f, availableWidth - ToolbarPadding * 2f);
            List<ComponentButtonInfo> buttons = BuildComponentButtons(gameObject);
            int rows = 1;
            float usedWidth = 0f;

            for (int i = 0; i < buttons.Count; i++)
            {
                float buttonWidth = buttons[i].MinWidth;
                float nextWidth = usedWidth <= 0f ? buttonWidth : usedWidth + ButtonGap + buttonWidth;
                if (usedWidth > 0f && nextWidth > rowWidth)
                {
                    rows++;
                    usedWidth = buttonWidth;
                }
                else
                {
                    usedWidth = nextWidth;
                }
            }

            return ToolbarPadding * 2f + ToolbarHeight + rows * ComponentButtonHeight + Mathf.Max(0, rows - 1) * ButtonGap;
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

        private static string ComponentName(Component component)
        {
            return component == null ? "MissingScript" : component.GetType().Name;
        }

        private static bool ShouldShowComponentButton(Component component)
        {
            return component == null || !component.hideFlags.HasFlag(HideFlags.HideInInspector);
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

            _clipboardButtonStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageOnly,
                fixedWidth = 0f,
                fixedHeight = 0f,
                normal = { background = _buttonTexture, textColor = LoogaEditorStyle.TextColor },
                hover = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                active = { background = _buttonActiveTexture, textColor = Color.white },
                focused = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _componentButtonStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { background = _buttonTexture, textColor = LoogaEditorStyle.TextColor },
                hover = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                active = { background = _buttonActiveTexture, textColor = Color.white },
                focused = { background = _buttonHoverTexture, textColor = LoogaEditorStyle.TextColor },
                padding = new RectOffset((int)ComponentButtonHorizontalPadding, (int)ComponentButtonHorizontalPadding, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _componentButtonSelectedStyle ??= new GUIStyle(_componentButtonStyle)
            {
                normal = { background = _buttonActiveTexture, textColor = Color.white },
                hover = { background = _buttonActiveTexture, textColor = Color.white },
                active = { background = _buttonActiveTexture, textColor = Color.white },
                focused = { background = _buttonActiveTexture, textColor = Color.white }
            };

            _countLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                normal = { textColor = LoogaEditorStyle.TextColor },
                padding = new RectOffset(1, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
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

        private readonly struct ComponentButtonInfo
        {
            public readonly int Id;
            public readonly GUIContent Content;
            public readonly float MinWidth;

            public ComponentButtonInfo(int id, GUIContent content, float minWidth)
            {
                Id = id;
                Content = content;
                MinWidth = minWidth;
            }
        }

        private sealed class InspectorToolbarContainer
        {
            private readonly PropertyInfo _lockedProperty;
            private readonly HashSet<int> _selectedComponentIds = new();
            private VisualElement _editorList;
            private IMGUIContainer _toolbar;
            private Object _inspectingObject;
            private string _componentSignature;
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

                RefreshComponentSignature(gameObject);
                float height = CalculateToolbarHeight(gameObject, Mathf.Max(1f, _editorList.resolvedStyle.width));
                _toolbar.style.height = height;
                _toolbar.style.minHeight = height;
                ApplyComponentFilter(gameObject);
            }

            public void RemoveToolbar()
            {
                _toolbar?.RemoveFromHierarchy();
            }

            private void RefreshSelectionIfNeeded()
            {
                bool locked = IsLocked;
                Object previousObject = _inspectingObject;
                if (!locked)
                    _inspectingObject = Selection.activeObject;
                else if (!_wasLocked || NeedsSelectionRefresh)
                    _inspectingObject ??= Selection.activeObject;

                if (previousObject != _inspectingObject)
                {
                    _selectedComponentIds.Clear();
                    _componentSignature = string.Empty;
                }

                _wasLocked = locked;
                NeedsSelectionRefresh = false;
            }

            private void RefreshComponentSignature(GameObject gameObject)
            {
                string signature = BuildComponentSignature(gameObject);
                if (signature == _componentSignature)
                    return;

                _componentSignature = signature;
                RemoveMissingSelections(gameObject);
            }

            private string BuildComponentSignature(GameObject gameObject)
            {
                Component[] components = gameObject.GetComponents<Component>();
                System.Text.StringBuilder builder = new(components.Length * 16);
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (!ShouldShowComponentButton(component))
                        continue;

                    builder.Append(component != null ? component.GetInstanceID() : 0);
                    builder.Append('|');
                    builder.Append(component != null ? component.GetType().FullName : "Missing");
                    builder.Append(';');
                }

                return builder.ToString();
            }

            private void RemoveMissingSelections(GameObject gameObject)
            {
                if (_selectedComponentIds.Count == 0)
                    return;

                HashSet<int> validIds = new();
                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (component != null && ShouldShowComponentButton(component))
                        validIds.Add(component.GetInstanceID());
                }

                _selectedComponentIds.RemoveWhere(id => !validIds.Contains(id));
            }

            private void CreateToolbar()
            {
                _toolbar = new IMGUIContainer(() =>
                {
                    DrawToolbar(ResolveGameObject(_inspectingObject), new[] { _inspectingObject }, _selectedComponentIds);
                    if (GUI.changed)
                    {
                        ApplyComponentFilter(ResolveGameObject(_inspectingObject));
                        Window.Repaint();
                    }
                })
                {
                    name = ToolbarName
                };
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

            private void ApplyComponentFilter(GameObject gameObject)
            {
                if (gameObject == null || _toolbar == null || _editorList == null)
                    return;

                Component[] components = gameObject.GetComponents<Component>();
                int toolbarIndex = _editorList.IndexOf(_toolbar);
                if (toolbarIndex < 0)
                    return;

                int componentIndex = 0;
                for (int i = toolbarIndex + 1; i < _editorList.childCount && componentIndex < components.Length; i++)
                {
                    VisualElement element = _editorList[i];
                    if (element == null || element.name == ToolbarName)
                        continue;

                    Component component = components[componentIndex++];
                    bool show = _selectedComponentIds.Count == 0 || component != null && _selectedComponentIds.Contains(component.GetInstanceID());
                    element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                }
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
