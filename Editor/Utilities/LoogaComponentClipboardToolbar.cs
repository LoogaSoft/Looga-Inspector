using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.VectorGraphics;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Adds a Looga-styled component clipboard above the first component in GameObject inspectors.
    /// The toolbar itself is drawn through IMGUI so it sits in Unity's inspector list like native inspector rows.
    /// </summary>
    [InitializeOnLoad]
    internal static class LoogaComponentClipboardToolbar
    {
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string ToolbarName = "Looga Component Clipboard Toolbar";
        private const int AllComponentsButtonId = -1;
        private const float ToolbarRowHeight = 22f;
        private const float ButtonGap = 2f;
        private const float ToolbarPadding = 1f;
        private const float DividerHeight = 1f;
        private const float ButtonWidth = 28f;
        private const float ButtonHeight = 20f;
        private const float IconSize = 13f;
        private const float ComponentButtonHeight = 23f;
        private const float ComponentButtonGap = 2f;
        private const float ComponentButtonHorizontalPadding = 6f;
        private const float ComponentIconSize = 14f;
        private const float ComponentRowTopPadding = 2f;
        private const float SearchFieldRightPadding = 2f;
        private const int RenderedIconSize = 32;
        private const string CopyIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/copy.svg";
        private const string PasteIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/clipboard-paste.svg";
        private const string PasteValuesIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/paste-values.svg";

        private static readonly List<InspectorToolbarContainer> Containers = new();
        private static readonly System.Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly Color ButtonIdleColor = LoogaEditorStyle.ListRowColor;
        private static readonly Color ButtonHoverColor = LoogaEditorStyle.ListHoverColor;
        private static readonly Color ComponentSelectedColor = LoogaEditorStyle.SelectionColor;
        private static readonly Color IconTintColor = new(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color DividerColor = new(0.16f, 0.16f, 0.16f, 1f);
        private static Texture2D _copyIcon;
        private static Texture2D _pasteIcon;
        private static Texture2D _pasteValuesIcon;
        private static Texture2D _idleTexture;
        private static Texture2D _hoverTexture;
        private static Texture2D _selectedTexture;
        private static Texture2D _toolbarTexture;
        private static GUIStyle _buttonStyle;
        private static GUIStyle _selectedButtonStyle;
        private static GUIStyle _componentTextStyle;

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

        private static void EnsureResources()
        {
            _copyIcon ??= RenderSvgIcon(CopyIconPath);
            _pasteIcon ??= RenderSvgIcon(PasteIconPath);
            _pasteValuesIcon ??= RenderSvgIcon(PasteValuesIconPath);
            _idleTexture ??= CreateTexture(ButtonIdleColor);
            _hoverTexture ??= CreateTexture(ButtonHoverColor);
            _selectedTexture ??= CreateTexture(ComponentSelectedColor);
            _toolbarTexture ??= CreateTexture(LoogaEditorStyle.BoxColor);

            _buttonStyle ??= CreateButtonStyle(_idleTexture, _hoverTexture);
            _selectedButtonStyle ??= CreateButtonStyle(_selectedTexture, _selectedTexture);
            _componentTextStyle ??= new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
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

        private static Texture2D RenderSvgIcon(string assetPath)
        {
            string absolutePath = ResolvePackageAssetPath(assetPath);
            if (!File.Exists(absolutePath))
                return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);

            using StringReader reader = new(File.ReadAllText(absolutePath));
            SVGParser.SceneInfo sceneInfo = SVGParser.ImportSVG(reader, 0f, 1f, 0, 0, false);
            List<VectorUtils.Geometry> geometry = VectorUtils.TessellateScene(sceneInfo.Scene, new VectorUtils.TessellationOptions
            {
                StepDistance = 100f,
                MaxCordDeviation = 0.1f,
                MaxTanAngleDeviation = 0.1f,
                SamplingStepSize = 0.01f
            }, null);
            Sprite sprite = VectorUtils.BuildSprite(geometry, 100f, VectorUtils.Alignment.Center, Vector2.zero, 128, true);
            Texture2D texture = VectorUtils.RenderSpriteToTexture2D(sprite, RenderedIconSize, RenderedIconSize, null, 4, true);
            texture.hideFlags = HideFlags.HideAndDontSave;
            return texture;
        }

        private static string ResolvePackageAssetPath(string assetPath)
        {
            UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(assetPath);
            if (packageInfo == null || string.IsNullOrWhiteSpace(packageInfo.resolvedPath))
                return Path.GetFullPath(assetPath);

            string packagePrefix = $"Packages/{packageInfo.name}/";
            string relativePath = assetPath.StartsWith(packagePrefix)
                ? assetPath[packagePrefix.Length..]
                : assetPath;
            return Path.Combine(packageInfo.resolvedPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static GUIStyle CreateButtonStyle(Texture2D normal, Texture2D hover)
        {
            return new GUIStyle(GUIStyle.none)
            {
                normal = { background = normal },
                hover = { background = hover },
                active = { background = normal },
                focused = { background = normal },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                border = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter
            };
        }

        private static bool DrawIconButton(Rect rect, Texture2D icon, string tooltip)
        {
            bool pressed = GUI.Button(rect, new GUIContent(string.Empty, tooltip), _buttonStyle);
            if (icon != null)
            {
                Rect iconRect = CenterRect(rect, IconSize, IconSize);
                Color previousColor = GUI.color;
                GUI.color = IconTintColor;
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
                GUI.color = previousColor;
            }

            return pressed;
        }

        private static Rect CenterRect(Rect parent, float width, float height)
        {
            return new Rect(
                Mathf.Round(parent.x + (parent.width - width) * 0.5f),
                Mathf.Round(parent.y + (parent.height - height) * 0.5f),
                width,
                height);
        }

        private sealed class InspectorToolbarContainer
        {
            private readonly PropertyInfo _lockedProperty;
            private readonly HashSet<int> _selectedComponentIds = new();
            private VisualElement _editorList;
            private IMGUIContainer _toolbar;
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

                RefreshToolbarHeight(gameObject);
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
                }

                _wasLocked = locked;
                NeedsSelectionRefresh = false;
            }

            private void CreateToolbar()
            {
                EnsureResources();

                _toolbar = new IMGUIContainer(DrawToolbar)
                {
                    name = ToolbarName
                };
                _toolbar.style.flexShrink = 0f;
                _toolbar.style.marginLeft = 0f;
                _toolbar.style.marginRight = 0f;
                _toolbar.style.marginTop = 0f;
                _toolbar.style.marginBottom = 0f;
                _toolbar.style.paddingLeft = 0f;
                _toolbar.style.paddingRight = 0f;
                _toolbar.style.paddingTop = 0f;
                _toolbar.style.paddingBottom = 0f;
                _toolbar.style.backgroundColor = LoogaEditorStyle.BoxColor;
                _toolbar.style.borderTopWidth = DividerHeight;
                _toolbar.style.borderBottomWidth = DividerHeight;
                _toolbar.style.borderTopColor = DividerColor;
                _toolbar.style.borderBottomColor = DividerColor;
            }

            private void DrawToolbar()
            {
                EnsureResources();

                GameObject gameObject = ResolveGameObject(_inspectingObject);
                if (gameObject == null)
                    return;

                Rect fullRect = _toolbar.contentRect;
                EditorGUI.DrawRect(fullRect, LoogaEditorStyle.BoxColor);
                EditorGUI.DrawRect(new Rect(fullRect.x, fullRect.y, fullRect.width, DividerHeight), DividerColor);
                EditorGUI.DrawRect(new Rect(fullRect.x, fullRect.yMax - DividerHeight, fullRect.width, DividerHeight), DividerColor);

                DrawActionRow(fullRect, gameObject);
                DrawComponentButtons(fullRect, gameObject);

                if (Event.current.type is EventType.MouseMove or EventType.MouseDrag)
                    _toolbar.MarkDirtyRepaint();
            }

            private void DrawActionRow(Rect fullRect, GameObject gameObject)
            {
                Rect rowRect = new(
                    fullRect.x + ToolbarPadding,
                    fullRect.y + ToolbarPadding,
                    fullRect.width - ToolbarPadding * 2f,
                    ToolbarRowHeight - ToolbarPadding * 2f);

                Rect buttonRect = new(rowRect.x, rowRect.y, ButtonWidth, ButtonHeight);
                if (DrawIconButton(buttonRect, _copyIcon, "Copy selected components"))
                {
                    LoogaComponentClipboard.CopyComponents(gameObject, _selectedComponentIds);
                    _toolbar.MarkDirtyRepaint();
                }

                float nextX = buttonRect.xMax + ButtonGap;
                if (LoogaComponentClipboard.HasPasteableComponents)
                {
                    buttonRect.x = nextX;
                    if (DrawIconButton(buttonRect, _pasteIcon, "Paste components"))
                    {
                        LoogaComponentClipboard.PasteComponents(new[] { _inspectingObject });
                        _toolbar.MarkDirtyRepaint();
                    }

                    nextX = buttonRect.xMax + ButtonGap;
                }

                if (LoogaComponentClipboard.HasClipboard)
                {
                    buttonRect.x = nextX;
                    if (DrawIconButton(buttonRect, _pasteValuesIcon, "Paste values into matching components"))
                    {
                        LoogaComponentClipboard.PasteValuesIntoMatchingComponents(new[] { _inspectingObject });
                        _toolbar.MarkDirtyRepaint();
                    }

                    nextX = buttonRect.xMax + ButtonGap;
                }

                Rect searchRect = new(
                    nextX,
                    rowRect.y,
                    Mathf.Max(0f, rowRect.xMax - nextX - SearchFieldRightPadding),
                    ButtonHeight);

                EditorGUI.BeginChangeCheck();
                string newSearchText = EditorGUI.TextField(searchRect, _searchText, EditorStyles.toolbarSearchField);
                if (EditorGUI.EndChangeCheck())
                {
                    _searchText = newSearchText ?? string.Empty;
                    RefreshToolbarHeight(gameObject);
                    ApplyComponentFilter(gameObject);
                }
            }

            private void DrawComponentButtons(Rect fullRect, GameObject gameObject)
            {
                List<ComponentButtonInfo> buttons = BuildVisibleComponentButtons(gameObject);
                if (buttons.Count == 0)
                    return;

                float availableWidth = fullRect.width - ToolbarPadding * 2f;
                float rowY = fullRect.y + ToolbarRowHeight + ComponentRowTopPadding;
                int index = 0;
                while (index < buttons.Count)
                {
                    int rowStart = index;
                    float rowWidth = 0f;
                    while (index < buttons.Count)
                    {
                        float nextWidth = buttons[index].MinWidth;
                        float projectedWidth = rowWidth <= 0f ? nextWidth : rowWidth + ComponentButtonGap + nextWidth;
                        if (projectedWidth > availableWidth && index > rowStart)
                            break;

                        rowWidth = projectedWidth;
                        index++;
                    }

                    DrawComponentButtonRow(buttons, rowStart, index, rowY, availableWidth);
                    rowY += ComponentButtonHeight + ComponentButtonGap;
                }
            }

            private void DrawComponentButtonRow(List<ComponentButtonInfo> buttons, int startIndex, int endIndex, float y, float availableWidth)
            {
                int count = endIndex - startIndex;
                float widthTotal = 0f;
                for (int i = startIndex; i < endIndex; i++)
                    widthTotal += buttons[i].MinWidth;

                float gapTotal = ComponentButtonGap * Mathf.Max(0, count - 1);
                float extraPerButton = count > 0 ? Mathf.Max(0f, availableWidth - widthTotal - gapTotal) / count : 0f;
                float x = ToolbarPadding;
                for (int i = startIndex; i < endIndex; i++)
                {
                    ComponentButtonInfo info = buttons[i];
                    float width = Mathf.Floor(info.MinWidth + extraPerButton);
                    Rect rect = new(x, y, width, ComponentButtonHeight);
                    DrawComponentButton(rect, info);
                    x += width + ComponentButtonGap;
                }
            }

            private void DrawComponentButton(Rect rect, ComponentButtonInfo info)
            {
                bool selected = IsComponentButtonSelected(info.ComponentId);
                GUIStyle style = selected ? _selectedButtonStyle : _buttonStyle;
                if (GUI.Button(rect, GUIContent.none, style))
                {
                    if (info.ComponentId == AllComponentsButtonId)
                        _selectedComponentIds.Clear();
                    else if (!_selectedComponentIds.Add(info.ComponentId))
                        _selectedComponentIds.Remove(info.ComponentId);

                    ApplyComponentFilter(ResolveGameObject(_inspectingObject));
                    _toolbar.MarkDirtyRepaint();
                }

                if (info.Icon != null)
                {
                    Rect iconRect = new(
                        rect.x + ComponentButtonHorizontalPadding,
                        Mathf.Round(rect.y + (rect.height - ComponentIconSize) * 0.5f),
                        ComponentIconSize,
                        ComponentIconSize);
                    GUI.DrawTexture(iconRect, info.Icon, ScaleMode.ScaleToFit, true);
                }

                float labelLeft = info.Icon != null
                    ? rect.x + ComponentButtonHorizontalPadding + ComponentIconSize + ButtonGap
                    : rect.x + ComponentButtonHorizontalPadding;
                Rect labelRect = new(labelLeft, rect.y, rect.xMax - labelLeft - ComponentButtonHorizontalPadding, rect.height);
                GUI.Label(labelRect, info.Label, _componentTextStyle);
            }

            private void RefreshToolbarHeight(GameObject gameObject)
            {
                string signature = BuildComponentSignature(gameObject);
                if (signature == _componentSignature && _toolbar != null && _toolbar.resolvedStyle.height > 0f)
                    return;

                _componentSignature = signature;
                float height = CalculateToolbarHeight(gameObject);
                _toolbar.style.height = height;
                _toolbar.style.minHeight = height;
                _toolbar.style.width = new StyleLength(StyleKeyword.Auto);
                _toolbar.MarkDirtyRepaint();
            }

            private float CalculateToolbarHeight(GameObject gameObject)
            {
                List<ComponentButtonInfo> buttons = BuildVisibleComponentButtons(gameObject);
                float availableWidth = Mathf.Max(1f, _editorList != null ? _editorList.layout.width - ToolbarPadding * 2f : 1f);
                int rowCount = 1;
                float rowWidth = 0f;
                for (int i = 0; i < buttons.Count; i++)
                {
                    float nextWidth = buttons[i].MinWidth;
                    float projectedWidth = rowWidth <= 0f ? nextWidth : rowWidth + ComponentButtonGap + nextWidth;
                    if (projectedWidth > availableWidth && rowWidth > 0f)
                    {
                        rowCount++;
                        rowWidth = nextWidth;
                        continue;
                    }

                    rowWidth = projectedWidth;
                }

                return ToolbarRowHeight + ComponentRowTopPadding + rowCount * (ComponentButtonHeight + ComponentButtonGap);
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

            private string BuildComponentSignature(GameObject gameObject)
            {
                Component[] components = gameObject.GetComponents<Component>();
                System.Text.StringBuilder builder = new(components.Length * 16);
                builder.Append(_searchText);
                builder.Append('|');
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
