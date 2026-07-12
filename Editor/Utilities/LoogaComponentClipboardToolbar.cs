using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.VectorGraphics;
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
        private const float ButtonVerticalInset = 2f;
        private const float IconSize = 13f;
        private const float CountLabelInset = 2f;
        private const float ComponentButtonHeight = 23f;
        private const float ComponentButtonGap = 2f;
        private const float ComponentButtonHorizontalPadding = 7f;
        private const float ComponentIconSize = 14f;
        private const float ComponentRowTopPadding = 1f;
        private const string CopyIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/copy.svg";
        private const string PasteIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/clipboard-paste.svg";
        private const string PasteValuesIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/paste-values.svg";

        private static readonly List<InspectorToolbarContainer> Containers = new();
        private static readonly System.Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static VectorImage _copyIcon;
        private static VectorImage _pasteIcon;
        private static VectorImage _pasteValuesIcon;
        private static readonly Color ButtonIdleColor = LoogaEditorStyle.ListRowColor;
        private static readonly Color ButtonHoverColor = LoogaEditorStyle.ListHoverColor;
        private static readonly Color ComponentSelectedColor = LoogaEditorStyle.SelectionColor;
        private static readonly Color IconTintColor = new(0.78f, 0.78f, 0.78f, 1f);

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

        private static Button CreateToolbarButton(VectorImage icon, string tooltip, System.Action clicked)
        {
            Button button = new(clicked)
            {
                tooltip = tooltip,
                focusable = false
            };
            button.style.width = ButtonWidth;
            button.style.height = ToolbarHeight - ButtonVerticalInset * 2f;
            button.style.marginLeft = 0f;
            button.style.marginRight = ButtonGap;
            button.style.marginTop = ButtonVerticalInset;
            button.style.marginBottom = ButtonVerticalInset;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.backgroundColor = ButtonIdleColor;
            button.style.borderTopWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderTopLeftRadius = 2f;
            button.style.borderTopRightRadius = 2f;
            button.style.borderBottomLeftRadius = 2f;
            button.style.borderBottomRightRadius = 2f;

            Image image = new()
            {
                vectorImage = icon,
                tintColor = IconTintColor,
                pickingMode = PickingMode.Ignore,
                scaleMode = ScaleMode.ScaleToFit
            };
            image.style.width = IconSize;
            image.style.height = IconSize;
            image.style.flexGrow = 0f;
            image.style.flexShrink = 0f;
            button.Add(image);

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = ButtonHoverColor);
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = ButtonIdleColor);
            button.RegisterCallback<MouseDownEvent>(_ => button.style.backgroundColor = ButtonIdleColor);
            button.RegisterCallback<MouseUpEvent>(_ => button.style.backgroundColor = button.enabledInHierarchy ? ButtonHoverColor : ButtonIdleColor);
            return button;
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
            return component == null ? "Missing Script" : ObjectNames.NicifyVariableName(component.GetType().Name);
        }

        private static void EnsureIcons()
        {
            _copyIcon ??= AssetDatabase.LoadAssetAtPath<VectorImage>(CopyIconPath);
            _pasteIcon ??= AssetDatabase.LoadAssetAtPath<VectorImage>(PasteIconPath);
            _pasteValuesIcon ??= AssetDatabase.LoadAssetAtPath<VectorImage>(PasteValuesIconPath);
        }

        private sealed class InspectorToolbarContainer
        {
            private readonly PropertyInfo _lockedProperty;
            private readonly HashSet<int> _selectedComponentIds = new();
            private VisualElement _editorList;
            private VisualElement _toolbar;
            private VisualElement _clipboardRow;
            private VisualElement _componentButtonGrid;
            private Button _pasteButton;
            private Button _pasteValuesButton;
            private Label _countLabel;
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

                UpdateToolbarState();
                RebuildComponentButtonsIfNeeded(gameObject);
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

            private void CreateToolbar()
            {
                EnsureIcons();

                _toolbar = new VisualElement
                {
                    name = ToolbarName
                };
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
                _toolbar.style.backgroundColor = LoogaEditorStyle.BoxColor;
                _toolbar.style.flexDirection = FlexDirection.Column;

                Button copyButton = CreateToolbarButton(_copyIcon, "Copy components", () =>
                {
                    GameObject gameObject = ResolveGameObject(_inspectingObject);
                    if (gameObject != null)
                        LoogaComponentClipboard.CopyComponents(gameObject);

                    UpdateToolbarState();
                });
                _pasteButton = CreateToolbarButton(_pasteIcon, "Paste components", () =>
                {
                    LoogaComponentClipboard.PasteComponents(new[] { _inspectingObject });
                    UpdateToolbarState();
                });
                _pasteValuesButton = CreateToolbarButton(_pasteValuesIcon, "Paste values into matching components", () =>
                {
                    LoogaComponentClipboard.PasteValuesIntoMatchingComponents(new[] { _inspectingObject });
                    UpdateToolbarState();
                });
                _countLabel = new Label
                {
                    pickingMode = PickingMode.Ignore
                };
                _countLabel.style.height = ToolbarHeight;
                _countLabel.style.marginLeft = CountLabelInset;
                _countLabel.style.paddingLeft = CountLabelInset;
                _countLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                _countLabel.style.color = LoogaEditorStyle.TextColor;
                _countLabel.style.fontSize = 12;

                _clipboardRow = new VisualElement();
                _clipboardRow.style.height = ToolbarHeight;
                _clipboardRow.style.minHeight = ToolbarHeight;
                _clipboardRow.style.flexDirection = FlexDirection.Row;
                _clipboardRow.style.alignItems = Align.Center;
                _clipboardRow.style.flexShrink = 0f;
                _clipboardRow.Add(copyButton);
                _clipboardRow.Add(_pasteButton);
                _clipboardRow.Add(_pasteValuesButton);
                _clipboardRow.Add(_countLabel);

                _componentButtonGrid = new VisualElement();
                _componentButtonGrid.style.flexDirection = FlexDirection.Row;
                _componentButtonGrid.style.flexWrap = Wrap.Wrap;
                _componentButtonGrid.style.paddingTop = ComponentRowTopPadding;
                _componentButtonGrid.style.paddingBottom = ComponentButtonGap;
                _componentButtonGrid.style.paddingLeft = 0f;
                _componentButtonGrid.style.paddingRight = 0f;

                _toolbar.Add(_clipboardRow);
                _toolbar.Add(_componentButtonGrid);
                UpdateToolbarState();
            }

            private void UpdateToolbarState()
            {
                if (_toolbar == null)
                    return;

                bool hasClipboard = LoogaComponentClipboard.HasClipboard;
                DisplayStyle pasteDisplay = hasClipboard ? DisplayStyle.Flex : DisplayStyle.None;
                _pasteButton.style.display = pasteDisplay;
                _pasteValuesButton.style.display = pasteDisplay;
                _countLabel.style.display = pasteDisplay;
                _countLabel.text = hasClipboard ? $"{LoogaComponentClipboard.CopiedCount} copied" : string.Empty;
            }

            private void RebuildComponentButtonsIfNeeded(GameObject gameObject)
            {
                string signature = BuildComponentSignature(gameObject);
                if (_componentButtonGrid == null || signature == _componentSignature)
                    return;

                _componentSignature = signature;
                _componentButtonGrid.Clear();

                Component[] components = gameObject.GetComponents<Component>();
                for (int i = 0; i < components.Length; i++)
                {
                    Component component = components[i];
                    if (!ShouldShowComponentButton(component))
                        continue;

                    _componentButtonGrid.Add(CreateComponentButton(component));
                }

                UpdateComponentButtonVisuals();
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

            private Button CreateComponentButton(Component component)
            {
                int componentId = component != null ? component.GetInstanceID() : 0;
                string label = ComponentName(component);
                GUIContent content = component != null ? EditorGUIUtility.ObjectContent(component, component.GetType()) : GUIContent.none;
                float minWidth = EditorStyles.label.CalcSize(new GUIContent(label)).x + ComponentIconSize + ComponentButtonHorizontalPadding * 2f + ButtonGap;

                Button button = new(() =>
                {
                    if (componentId == 0)
                        return;

                    if (!_selectedComponentIds.Add(componentId))
                        _selectedComponentIds.Remove(componentId);

                    UpdateComponentButtonVisuals();
                    ApplyComponentFilter(ResolveGameObject(_inspectingObject));
                })
                {
                    tooltip = label,
                    userData = componentId,
                    focusable = false
                };
                button.style.minWidth = minWidth;
                button.style.height = ComponentButtonHeight;
                button.style.flexGrow = 1f;
                button.style.marginLeft = 0f;
                button.style.marginRight = ComponentButtonGap;
                button.style.marginTop = 0f;
                button.style.marginBottom = ComponentButtonGap;
                button.style.paddingLeft = ComponentButtonHorizontalPadding;
                button.style.paddingRight = ComponentButtonHorizontalPadding;
                button.style.paddingTop = 0f;
                button.style.paddingBottom = 0f;
                button.style.flexDirection = FlexDirection.Row;
                button.style.alignItems = Align.Center;
                button.style.justifyContent = Justify.Center;
                button.style.backgroundColor = ButtonIdleColor;
                button.style.borderTopWidth = 0f;
                button.style.borderRightWidth = 0f;
                button.style.borderBottomWidth = 0f;
                button.style.borderLeftWidth = 0f;
                button.style.borderTopLeftRadius = 2f;
                button.style.borderTopRightRadius = 2f;
                button.style.borderBottomLeftRadius = 2f;
                button.style.borderBottomRightRadius = 2f;

                Image icon = new()
                {
                    image = content.image,
                    pickingMode = PickingMode.Ignore,
                    scaleMode = ScaleMode.ScaleToFit
                };
                icon.style.width = ComponentIconSize;
                icon.style.height = ComponentIconSize;
                icon.style.marginRight = ButtonGap;
                icon.style.flexGrow = 0f;
                icon.style.flexShrink = 0f;

                Label name = new(label)
                {
                    pickingMode = PickingMode.Ignore
                };
                name.style.color = LoogaEditorStyle.TextColor;
                name.style.fontSize = 12;
                name.style.unityTextAlign = TextAnchor.MiddleCenter;
                name.style.flexShrink = 0f;

                button.Add(icon);
                button.Add(name);
                button.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    if (IsComponentButtonSelected(button))
                        return;

                    button.style.backgroundColor = ButtonHoverColor;
                });
                button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = IsComponentButtonSelected(button) ? ComponentSelectedColor : ButtonIdleColor);
                button.RegisterCallback<MouseDownEvent>(_ => button.style.backgroundColor = IsComponentButtonSelected(button) ? ComponentSelectedColor : ButtonIdleColor);
                button.RegisterCallback<MouseUpEvent>(_ => button.style.backgroundColor = IsComponentButtonSelected(button) ? ComponentSelectedColor : ButtonHoverColor);
                return button;
            }

            private bool ShouldShowComponentButton(Component component)
            {
                return component == null || !component.hideFlags.HasFlag(HideFlags.HideInInspector);
            }

            private bool IsComponentButtonSelected(Button button)
            {
                return button.userData is int componentId && _selectedComponentIds.Contains(componentId);
            }

            private void UpdateComponentButtonVisuals()
            {
                if (_componentButtonGrid == null)
                    return;

                for (int i = 0; i < _componentButtonGrid.childCount; i++)
                {
                    if (_componentButtonGrid[i] is not Button button)
                        continue;

                    button.style.backgroundColor = IsComponentButtonSelected(button) ? ComponentSelectedColor : ButtonIdleColor;
                }
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
