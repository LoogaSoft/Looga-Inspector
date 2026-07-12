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
    /// The toolbar uses UI Toolkit so SVG icons, hover states, and inspector layout all stay native and stable.
    /// </summary>
    [InitializeOnLoad]
    internal static class LoogaComponentClipboardToolbar
    {
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string ToolbarName = "Looga Component Clipboard Toolbar";
        private const int AllComponentsButtonId = -1;
        private const float ToolbarPadding = 1f;
        private const float DividerHeight = 1f;
        private const float ActionButtonWidth = 28f;
        private const float ActionRowHeight = 20f;
        private const float ButtonGap = 2f;
        private const float IconSize = 13f;
        private const float ComponentButtonHeight = 23f;
        private const float ComponentButtonHorizontalPadding = 6f;
        private const float ComponentIconSize = 14f;
        private const float ComponentRowsTopPadding = 0f;
        private const string CopyIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/copy.svg";
        private const string PasteIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/clipboard-paste.svg";
        private const string PasteValuesIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/paste-values.svg";

        private static readonly List<InspectorToolbarContainer> Containers = new();
        private static readonly System.Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly Color ToolbarColor = LoogaEditorStyle.BoxColor;
        private static readonly Color ButtonIdleColor = LoogaEditorStyle.ListRowColor;
        private static readonly Color ButtonHoverColor = LoogaEditorStyle.ListHoverColor;
        private static readonly Color ComponentSelectedColor = LoogaEditorStyle.SelectionColor;
        private static readonly Color IconTintColor = new(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color DividerColor = new(0.09f, 0.09f, 0.09f, 1f);
        private static Object _copyIcon;
        private static Object _pasteIcon;
        private static Object _pasteValuesIcon;

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

        private static Object CopyIcon()
        {
            _copyIcon ??= LoadPackageIcon(CopyIconPath);
            return _copyIcon;
        }

        private static Object PasteIcon()
        {
            _pasteIcon ??= LoadPackageIcon(PasteIconPath);
            return _pasteIcon;
        }

        private static Object PasteValuesIcon()
        {
            _pasteValuesIcon ??= LoadPackageIcon(PasteValuesIconPath);
            return _pasteValuesIcon;
        }

        private static Object LoadPackageIcon(string assetPath)
        {
            Object icon = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
            if (icon != null)
                return icon;

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static void SetIconImage(Image image, Object icon, Color tint)
        {
            image.image = null;
            image.vectorImage = null;
            image.tintColor = tint;

            if (icon is Texture2D textureIcon)
                image.image = textureIcon;
            else if (icon is VectorImage vectorIcon)
                image.vectorImage = vectorIcon;
        }

        private static Button CreateIconButton(Object icon, string tooltip, System.Action clicked)
        {
            Image iconImage = new()
            {
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };
            SetIconImage(iconImage, icon, IconTintColor);
            iconImage.style.width = IconSize;
            iconImage.style.height = IconSize;
            iconImage.style.flexShrink = 0f;
            iconImage.style.alignSelf = Align.Center;

            Button button = new(clicked)
            {
                tooltip = tooltip
            };
            button.Add(iconImage);
            button.style.width = ActionButtonWidth;
            button.style.height = ActionRowHeight;
            button.style.marginLeft = 0f;
            button.style.marginRight = ButtonGap;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.backgroundColor = ButtonIdleColor;
            ClearBorderAndRadius(button);
            RegisterHover(button, ButtonIdleColor, ButtonHoverColor);
            return button;
        }

        private static void RegisterHover(VisualElement element, Color idleColor, Color hoverColor)
        {
            element.RegisterCallback<MouseEnterEvent>(_ => element.style.backgroundColor = hoverColor);
            element.RegisterCallback<MouseLeaveEvent>(_ => element.style.backgroundColor = idleColor);
            element.RegisterCallback<MouseDownEvent>(_ => element.style.backgroundColor = idleColor);
            element.RegisterCallback<MouseUpEvent>(_ => element.style.backgroundColor = hoverColor);
        }

        private static void ClearBorderAndRadius(VisualElement element)
        {
            element.style.borderTopWidth = 0f;
            element.style.borderRightWidth = 0f;
            element.style.borderBottomWidth = 0f;
            element.style.borderLeftWidth = 0f;
            element.style.borderTopLeftRadius = 0f;
            element.style.borderTopRightRadius = 0f;
            element.style.borderBottomLeftRadius = 0f;
            element.style.borderBottomRightRadius = 0f;
        }

        private sealed class InspectorToolbarContainer
        {
            private readonly PropertyInfo _lockedProperty;
            private readonly HashSet<int> _selectedComponentIds = new();
            private VisualElement _editorList;
            private VisualElement _toolbar;
            private VisualElement _componentRows;
            private Button _pasteButton;
            private Button _pasteValuesButton;
            private TextField _searchField;
            private Object _inspectingObject;
            private string _componentSignature;
            private string _searchText = string.Empty;
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

                RefreshActionButtons();
                RefreshComponentRows(gameObject);
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
                    _searchText = string.Empty;
                    if (_searchField != null)
                        _searchField.SetValueWithoutNotify(string.Empty);
                }

                _wasLocked = locked;
                NeedsSelectionRefresh = false;
            }

            private void CreateToolbar()
            {
                _toolbar = new VisualElement
                {
                    name = ToolbarName,
                    pickingMode = PickingMode.Position
                };
                _toolbar.style.flexShrink = 0f;
                _toolbar.style.flexDirection = FlexDirection.Column;
                _toolbar.style.backgroundColor = ToolbarColor;
                _toolbar.style.marginLeft = 0f;
                _toolbar.style.marginRight = 0f;
                _toolbar.style.marginTop = 0f;
                _toolbar.style.marginBottom = 0f;
                _toolbar.style.paddingLeft = ToolbarPadding;
                _toolbar.style.paddingRight = ToolbarPadding;
                _toolbar.style.paddingTop = ToolbarPadding;
                _toolbar.style.paddingBottom = ToolbarPadding;
VisualElement actionRow = new();
                actionRow.style.flexDirection = FlexDirection.Row;
                actionRow.style.height = ActionRowHeight;
                actionRow.style.marginBottom = ButtonGap;
                actionRow.style.alignItems = Align.Center;

                Button copyButton = CreateIconButton(CopyIcon(), "Copy selected components", () =>
                {
                    GameObject gameObject = ResolveGameObject(_inspectingObject);
                    if (gameObject == null)
                        return;

                    LoogaComponentClipboard.CopyComponents(gameObject, _selectedComponentIds);
                    RefreshActionButtons();
                });

                _pasteButton = CreateIconButton(PasteIcon(), "Paste components", () =>
                {
                    LoogaComponentClipboard.PasteComponents(new[] { _inspectingObject });
                    RefreshActionButtons();
                });

                _pasteValuesButton = CreateIconButton(PasteValuesIcon(), "Paste values into matching components", () =>
                {
                    LoogaComponentClipboard.PasteValuesIntoMatchingComponents(new[] { _inspectingObject });
                    RefreshActionButtons();
                });

                _searchField = new TextField
                {
                    value = _searchText
                };
                _searchField.style.flexGrow = 1f;
                _searchField.style.height = ActionRowHeight;
                _searchField.style.marginLeft = 0f;
                _searchField.style.marginRight = 0f;
                _searchField.style.marginTop = 0f;
                _searchField.style.marginBottom = 0f;
                _searchField.RegisterValueChangedCallback(evt =>
                {
                    _searchText = evt.newValue ?? string.Empty;
                    _componentSignature = string.Empty;
                    GameObject gameObject = ResolveGameObject(_inspectingObject);
                    RefreshComponentRows(gameObject);
                    ApplyComponentFilter(gameObject);
                });

                actionRow.Add(copyButton);
                actionRow.Add(_pasteButton);
                actionRow.Add(_pasteValuesButton);
                actionRow.Add(_searchField);

                _componentRows = new VisualElement();
                _componentRows.style.flexDirection = FlexDirection.Column;

                _toolbar.Add(actionRow);
                _toolbar.Add(_componentRows);
            }

            private void RefreshActionButtons()
            {
                if (_pasteButton == null || _pasteValuesButton == null)
                    return;

                _pasteButton.style.display = LoogaComponentClipboard.HasPasteableComponents ? DisplayStyle.Flex : DisplayStyle.None;
                _pasteValuesButton.style.display = LoogaComponentClipboard.HasClipboard ? DisplayStyle.Flex : DisplayStyle.None;
            }

            private void RefreshComponentRows(GameObject gameObject)
            {
                if (gameObject == null || _componentRows == null)
                    return;

                string signature = BuildToolbarSignature(gameObject);
                if (signature == _componentSignature)
                    return;

                _componentSignature = signature;
                _componentRows.Clear();

                List<ComponentButtonInfo> buttons = BuildVisibleComponentButtons(gameObject);
                if (buttons.Count == 0)
                    return;

                float availableWidth = Mathf.Max(1f, _editorList != null ? _editorList.layout.width - ToolbarPadding * 2f : 1f);
                int index = 0;
                while (index < buttons.Count)
                {
                    int rowStart = index;
                    float rowWidth = 0f;
                    while (index < buttons.Count)
                    {
                        float nextWidth = buttons[index].MinWidth;
                        float projectedWidth = rowWidth <= 0f ? nextWidth : rowWidth + ButtonGap + nextWidth;
                        if (projectedWidth > availableWidth && index > rowStart)
                            break;

                        rowWidth = projectedWidth;
                        index++;
                    }

                    AddComponentButtonRow(buttons, rowStart, index, availableWidth, index < buttons.Count);
                }
            }

            private void AddComponentButtonRow(List<ComponentButtonInfo> buttons, int startIndex, int endIndex, float availableWidth, bool fillRow)
            {
                int count = endIndex - startIndex;
                float widthTotal = 0f;
                for (int i = startIndex; i < endIndex; i++)
                    widthTotal += buttons[i].MinWidth;

                float gapTotal = ButtonGap * Mathf.Max(0, count - 1);
                float extraPerButton = fillRow && count > 0 ? Mathf.Max(0f, availableWidth - widthTotal - gapTotal) / count : 0f;

                VisualElement row = new();
                row.style.flexDirection = FlexDirection.Row;
                row.style.height = ComponentButtonHeight;
                row.style.marginTop = _componentRows.childCount == 0 ? ComponentRowsTopPadding : ButtonGap;

                for (int i = startIndex; i < endIndex; i++)
                {
                    ComponentButtonInfo info = buttons[i];
                    float width = Mathf.Floor(info.MinWidth + extraPerButton);
                    Button button = CreateComponentButton(info, width);
                    if (i < endIndex - 1)
                        button.style.marginRight = ButtonGap;

                    row.Add(button);
                }

                _componentRows.Add(row);
            }

            private Button CreateComponentButton(ComponentButtonInfo info, float width)
            {
                bool selected = IsComponentButtonSelected(info.ComponentId);
                Color idleColor = selected ? ComponentSelectedColor : ButtonIdleColor;
                Color hoverColor = selected ? ComponentSelectedColor : ButtonHoverColor;

                Button button = new(() =>
                {
                    if (info.ComponentId == AllComponentsButtonId)
                        _selectedComponentIds.Clear();
                    else if (!_selectedComponentIds.Add(info.ComponentId))
                        _selectedComponentIds.Remove(info.ComponentId);

                    _componentSignature = string.Empty;
                    GameObject gameObject = ResolveGameObject(_inspectingObject);
                    RefreshComponentRows(gameObject);
                    ApplyComponentFilter(gameObject);
                });
                button.style.width = width;
                button.style.height = ComponentButtonHeight;
                button.style.marginLeft = 0f;
                button.style.marginRight = 0f;
                button.style.marginTop = 0f;
                button.style.marginBottom = 0f;
                button.style.paddingLeft = ComponentButtonHorizontalPadding;
                button.style.paddingRight = ComponentButtonHorizontalPadding;
                button.style.paddingTop = 0f;
                button.style.paddingBottom = 0f;
                button.style.flexDirection = FlexDirection.Row;
                button.style.alignItems = Align.Center;
                button.style.justifyContent = Justify.Center;
                button.style.backgroundColor = idleColor;
                ClearBorderAndRadius(button);
                RegisterHover(button, idleColor, hoverColor);

                if (info.Icon != null)
                {
                    Image icon = new()
                    {
                        image = info.Icon,
                        pickingMode = PickingMode.Ignore,
                        scaleMode = ScaleMode.ScaleToFit
                    };
                    icon.style.width = ComponentIconSize;
                    icon.style.height = ComponentIconSize;
                    icon.style.marginRight = ButtonGap;
                    icon.style.flexShrink = 0f;
                    button.Add(icon);
                }

                Label label = new(info.Label)
                {
                    pickingMode = PickingMode.Ignore
                };
                label.style.unityTextAlign = TextAnchor.MiddleCenter;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.fontSize = 11;
                label.style.color = LoogaEditorStyle.TextColor;
                label.style.flexShrink = 1f;
                button.Add(label);

                return button;
            }

            private List<ComponentButtonInfo> BuildVisibleComponentButtons(GameObject gameObject)
            {
                List<ComponentButtonInfo> buttons = new()
                {
                    ComponentButtonInfo.All()
                };

                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (!ShouldShowComponentButton(component) || !MatchesSearch(component))
                        continue;

                    buttons.Add(ComponentButtonInfo.From(component));
                }

                return buttons;
            }

            private string BuildToolbarSignature(GameObject gameObject)
            {
                Component[] components = gameObject.GetComponents<Component>();
                System.Text.StringBuilder builder = new(components.Length * 24);
                builder.Append(_searchText);
                builder.Append('|');
                builder.Append(Mathf.RoundToInt(_editorList != null ? _editorList.layout.width : 0f));
                builder.Append('|');
                builder.Append(_selectedComponentIds.Count);
                builder.Append('|');
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (!ShouldShowComponentButton(component))
                        continue;

                    int id = component != null ? component.GetInstanceID() : 0;
                    builder.Append(id);
                    builder.Append(':');
                    builder.Append(component != null ? component.GetType().FullName : "Missing");
                    builder.Append(':');
                    builder.Append(_selectedComponentIds.Contains(id));
                    builder.Append(';');
                }

                return builder.ToString();
            }

            private bool ShouldShowComponentButton(Component component)
            {
                return component == null || !component.hideFlags.HasFlag(HideFlags.HideInInspector);
            }

            private bool IsComponentButtonSelected(int componentId)
            {
                return componentId == AllComponentsButtonId
                    ? _selectedComponentIds.Count == 0
                    : _selectedComponentIds.Contains(componentId);
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
                    bool show = MatchesSelection(component) && MatchesSearch(component);
                    element.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }

            private bool MatchesSelection(Component component)
            {
                return _selectedComponentIds.Count == 0 || component != null && _selectedComponentIds.Contains(component.GetInstanceID());
            }

            private bool MatchesSearch(Component component)
            {
                return string.IsNullOrWhiteSpace(_searchText) || ComponentName(component).IndexOf(_searchText, System.StringComparison.OrdinalIgnoreCase) >= 0;
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

        private readonly struct ComponentButtonInfo
        {
            public readonly int ComponentId;
            public readonly string Label;
            public readonly Texture Icon;
            public readonly float MinWidth;

            private ComponentButtonInfo(int componentId, string label, Texture icon, float minWidth)
            {
                ComponentId = componentId;
                Label = label;
                Icon = icon;
                MinWidth = minWidth;
            }

            public static ComponentButtonInfo All()
            {
                string label = "All";
                float minWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(label)).x + ComponentButtonHorizontalPadding * 2f;
                return new ComponentButtonInfo(AllComponentsButtonId, label, null, minWidth);
            }

            public static ComponentButtonInfo From(Component component)
            {
                int componentId = component != null ? component.GetInstanceID() : 0;
                string label = ComponentName(component);
                Texture icon = component != null ? EditorGUIUtility.ObjectContent(component, component.GetType()).image : null;
                float iconWidth = icon != null ? ComponentIconSize + ButtonGap : 0f;
                float minWidth = EditorStyles.boldLabel.CalcSize(new GUIContent(label)).x + iconWidth + ComponentButtonHorizontalPadding * 2f;
                return new ComponentButtonInfo(componentId, label, icon, minWidth);
            }
        }
    }
}