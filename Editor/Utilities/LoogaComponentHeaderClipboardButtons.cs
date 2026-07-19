using System;
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
    /// Experimental component-header clipboard buttons. Unity does not expose a public API for the built-in
    /// component header icon cluster, so this probes the Inspector UIElements tree and fails silently if it changes.
    /// </summary>
    [InitializeOnLoad]
    internal static class LoogaComponentHeaderClipboardButtons
    {
        private const string InspectorListClassName = "unity-inspector-editors-list";
        private const string ButtonContainerName = "looga-component-header-clipboard-buttons";
        private const string CopyButtonName = "looga-component-header-copy-button";
        private const string PasteButtonName = "looga-component-header-paste-button";
        private const string CopyIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/copy.svg";
        private const string PasteIconPath = "Packages/com.loogasoft.loogainspector/Editor/Icons/Remix/clipboard-paste.svg";
        private const float ButtonSize = 15f;
        private const float IconSize = 11f;
        private const float ButtonGap = 2f;
        private const double CopySuccessSeconds = 1.0d;
        private static readonly Color ButtonIdleColor = new(0f, 0f, 0f, 0f);
        private static readonly Color IconTintColor = new(0.78f, 0.78f, 0.78f, 1f);
        private static readonly Color SuccessIconTintColor = new(0.38f, 0.82f, 0.42f, 1f);
        private static readonly Color ButtonHoverColor = new(0.58f, 0.58f, 0.58f, 0.78f);
        private const float RightOffset = 66f;
        private const float TopOffset = 4f;

        private static readonly Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly List<VisualElement> ScratchElements = new();
        private static readonly Dictionary<int, HeaderCandidate> CandidateByComponent = new();
        private static Object _copyIcon;
        private static Object _pasteIcon;
        private static Texture2D _generatedPasteIcon;
        private static Texture2D _checkIcon;
        private static int _copySuccessComponentId;
        private static double _copySuccessUntil;

        static LoogaComponentHeaderClipboardButtons()
        {
            EditorApplication.update -= RefreshInspectorButtons;
            EditorApplication.update += RefreshInspectorButtons;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }

        private static void Dispose()
        {
            EditorApplication.update -= RefreshInspectorButtons;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            DestroyGeneratedTexture(ref _generatedPasteIcon);
            DestroyGeneratedTexture(ref _checkIcon);
            ScratchElements.Clear();
            CandidateByComponent.Clear();
        }

        private static void DestroyGeneratedTexture(ref Texture2D texture)
        {
            if (texture == null)
                return;

            Object.DestroyImmediate(texture);
            texture = null;
        }

        private static void RefreshInspectorButtons()
        {
            if (AllInspectorsField == null || InspectorWindowType == null)
                return;

            if (AllInspectorsField.GetValue(null) is not IList windows)
                return;

            for (int i = 0; i < windows.Count; i++)
            {
                if (windows[i] is not EditorWindow inspectorWindow || inspectorWindow.rootVisualElement == null)
                    continue;

                VisualElement editorList = inspectorWindow.rootVisualElement.Q(null, InspectorListClassName);
                if (editorList == null)
                    continue;

                InjectIntoEditorTree(editorList);
            }
        }

        private static void InjectIntoEditorTree(VisualElement root)
        {
            ScratchElements.Clear();
            CandidateByComponent.Clear();
            CollectElements(root, ScratchElements);

            for (int i = 0; i < ScratchElements.Count; i++)
            {
                VisualElement element = ScratchElements[i];
                if (!IsLikelyEditorContainer(element) || !TryGetEditor(element, out UnityEditor.Editor editor))
                    continue;

                if (editor.target is not Component component)
                    continue;

                int instanceId = component.GetInstanceID();
                HeaderCandidate candidate = new(element, component, editor.targets, HeaderY(element), IsPreferredEditorElement(element));
                if (!CandidateByComponent.TryGetValue(instanceId, out HeaderCandidate current) || IsBetterCandidate(candidate, current))
                    CandidateByComponent[instanceId] = candidate;
            }

            RemoveStaleInjectedContainers(root);

            foreach (HeaderCandidate candidate in CandidateByComponent.Values)
                EnsureButtonContainer(candidate.Element, candidate.Component, candidate.Targets);
        }

        private static void RemoveStaleInjectedContainers(VisualElement root)
        {
            HashSet<VisualElement> activeElements = new();
            foreach (HeaderCandidate candidate in CandidateByComponent.Values)
                activeElements.Add(candidate.Element);

            ScratchElements.Clear();
            root.Query<VisualElement>(name: ButtonContainerName).ForEach(element => ScratchElements.Add(element));
            for (int i = 0; i < ScratchElements.Count; i++)
            {
                VisualElement container = ScratchElements[i];
                if (!activeElements.Contains(container.parent))
                    container.RemoveFromHierarchy();
            }
        }

        private static bool IsBetterCandidate(HeaderCandidate candidate, HeaderCandidate current)
        {
            if (candidate.Preferred != current.Preferred)
                return candidate.Preferred;

            return candidate.Y < current.Y;
        }

        private static float HeaderY(VisualElement element)
        {
            Rect worldBound = element.worldBound;
            return float.IsNaN(worldBound.y) ? float.MaxValue : worldBound.y;
        }

        private static void CollectElements(VisualElement element, List<VisualElement> elements)
        {
            if (element == null)
                return;

            elements.Add(element);
            for (int i = 0; i < element.childCount; i++)
                CollectElements(element[i], elements);
        }

        private static bool IsLikelyEditorContainer(VisualElement element)
        {
            Type type = element.GetType();
            string typeName = type.Name;
            if (typeName.IndexOf("EditorElement", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (typeName.IndexOf("InspectorElement", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return element.ClassListContains("unity-inspector-element");
        }

        private static bool IsPreferredEditorElement(VisualElement element)
        {
            return element.GetType().Name.IndexOf("EditorElement", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool TryGetEditor(VisualElement element, out UnityEditor.Editor editor)
        {
            editor = null;
            Type type = element.GetType();
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            while (type != null)
            {
                FieldInfo[] fields = type.GetFields(Flags);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (!typeof(UnityEditor.Editor).IsAssignableFrom(fields[i].FieldType))
                        continue;

                    editor = fields[i].GetValue(element) as UnityEditor.Editor;
                    if (editor != null)
                        return true;
                }

                PropertyInfo[] properties = type.GetProperties(Flags);
                for (int i = 0; i < properties.Length; i++)
                {
                    if (!typeof(UnityEditor.Editor).IsAssignableFrom(properties[i].PropertyType) || properties[i].GetIndexParameters().Length > 0)
                        continue;

                    try
                    {
                        editor = properties[i].GetValue(element) as UnityEditor.Editor;
                    }
                    catch
                    {
                        editor = null;
                    }

                    if (editor != null)
                        return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        private static void EnsureButtonContainer(VisualElement editorElement, Component component, Object[] targets)
        {
            VisualElement existingContainer = editorElement.Q<VisualElement>(name: ButtonContainerName);
            if (existingContainer != null)
            {
                int componentId = component.GetInstanceID();
                if (existingContainer.userData is int existingComponentId && existingComponentId == componentId)
                {
                    UpdateCopyButton(existingContainer, component);
                    UpdatePasteButton(existingContainer, targets);
                    return;
                }

                existingContainer.RemoveFromHierarchy();
            }

            VisualElement container = new()
            {
                name = ButtonContainerName,
                pickingMode = PickingMode.Position,
                userData = component.GetInstanceID()
            };
            container.style.position = Position.Absolute;
            container.style.top = TopOffset;
            container.style.right = RightOffset;
            container.style.width = ButtonSize * 2f + ButtonGap;
            container.style.height = ButtonSize;
            container.style.flexDirection = FlexDirection.Row;

            bool showCopySuccess = IsCopySuccessActive(component);
            Object copyIcon = showCopySuccess ? GetCheckIcon() : GetCopyIcon();
            Color copyTint = showCopySuccess ? SuccessIconTintColor : IconTintColor;
            Button copyButton = CreateHeaderButton(CopyButtonName, copyIcon, "Copy component", () =>
            {
                LoogaComponentClipboard.CopyComponent(component);
                MarkCopySuccess(component);
            }, copyTint, true, GetCopyIcon());
            Button pasteButton = CreateHeaderButton(PasteButtonName, GetPasteIcon(), "Paste values into this component", () => LoogaComponentClipboard.PasteValuesIntoComponents(targets), IconTintColor);
            pasteButton.style.marginLeft = ButtonGap;

            container.Add(copyButton);
            container.Add(pasteButton);
            editorElement.Add(container);
            UpdatePasteButton(container, targets);
        }

        private static Button CreateHeaderButton(string name, Object icon, string tooltip, Action clicked, Color iconTint, bool flashSuccess = false, Object restoreIcon = null)
        {
            Image iconImage = null;
            Button button = new(() =>
            {
                clicked?.Invoke();
                if (flashSuccess && iconImage != null)
                    FlashSuccessIcon(iconImage, restoreIcon ?? icon);
            })
            {
                name = name,
                tooltip = tooltip,
                text = icon == null ? tooltip[..1] : string.Empty
            };
            button.style.width = ButtonSize;
            button.style.height = ButtonSize;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.backgroundColor = ButtonIdleColor;
            button.style.borderTopWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderTopLeftRadius = 2f;
            button.style.borderTopRightRadius = 2f;
            button.style.borderBottomLeftRadius = 2f;
            button.style.borderBottomRightRadius = 2f;

            if (icon != null)
            {
                iconImage = new Image
                {
                    pickingMode = PickingMode.Ignore,
                    scaleMode = ScaleMode.ScaleToFit
                };
                SetIconImage(iconImage, icon, iconTint);
                iconImage.style.width = IconSize;
                iconImage.style.height = IconSize;
                iconImage.style.flexGrow = 0f;
                iconImage.style.flexShrink = 0f;
                iconImage.style.alignSelf = Align.Center;
                button.Add(iconImage);
            }

            button.RegisterCallback<MouseEnterEvent>(_ =>
            {
                if (button.enabledInHierarchy)
                    button.style.backgroundColor = ButtonHoverColor;
            });
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = ButtonIdleColor);
            button.RegisterCallback<MouseDownEvent>(_ => button.style.backgroundColor = ButtonIdleColor);
            button.RegisterCallback<MouseUpEvent>(_ => button.style.backgroundColor = button.enabledInHierarchy ? ButtonHoverColor : ButtonIdleColor);
            return button;
        }

        private static void UpdateCopyButton(VisualElement container, Component component)
        {
            Button copyButton = container.Q<Button>(name: CopyButtonName);
            Image image = copyButton?.Q<Image>();
            if (image == null)
                return;

            bool showCopySuccess = IsCopySuccessActive(component);
            SetIconImage(image, showCopySuccess ? GetCheckIcon() : GetCopyIcon(), showCopySuccess ? SuccessIconTintColor : IconTintColor);
        }

        private static void FlashSuccessIcon(Image image, Object normalIcon)
        {
            SetIconImage(image, GetCheckIcon(), SuccessIconTintColor);
            image.schedule.Execute(() => SetIconImage(image, normalIcon, IconTintColor)).StartingIn(1000);
        }

        private static void MarkCopySuccess(Component component)
        {
            if (component == null)
                return;

            _copySuccessComponentId = component.GetInstanceID();
            _copySuccessUntil = EditorApplication.timeSinceStartup + CopySuccessSeconds;
        }

        private static bool IsCopySuccessActive(Component component)
        {
            if (component == null || component.GetInstanceID() != _copySuccessComponentId)
                return false;

            if (EditorApplication.timeSinceStartup <= _copySuccessUntil)
                return true;

            _copySuccessComponentId = 0;
            _copySuccessUntil = 0d;
            return false;
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

        private static void UpdatePasteButton(VisualElement container, Object[] targets)
        {
            Button pasteButton = container.Q<Button>(name: PasteButtonName);
            if (pasteButton == null)
                return;

            pasteButton.SetEnabled(LoogaComponentClipboard.CanPasteValuesIntoComponents(targets));
            if (!pasteButton.enabledInHierarchy)
                pasteButton.style.backgroundColor = ButtonIdleColor;
        }

        private static Object GetCopyIcon()
        {
            _copyIcon ??= LoadPackageIcon(CopyIconPath);
            return _copyIcon != null ? _copyIcon : EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_TreeEditor.Duplicate" : "TreeEditor.Duplicate").image as Texture2D;
        }

        private static Object GetPasteIcon()
        {
            _pasteIcon ??= LoadPackageIcon(PasteIconPath);
            return _pasteIcon != null ? _pasteIcon : GetGeneratedPasteIcon();
        }

        private static Object LoadPackageIcon(string assetPath)
        {
            Object icon = AssetDatabase.LoadAssetAtPath<VectorImage>(assetPath);
            if (icon != null)
                return icon;

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static Texture2D GetCheckIcon()
        {
            if (_checkIcon != null)
                return _checkIcon;

            Color32 clear = new(0, 0, 0, 0);
            Color32 ink = new(255, 255, 255, 255);
            _checkIcon = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = "Looga Component Copied Check Icon",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };

            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            void Pixel(int x, int y)
            {
                if (x < 0 || x >= 16 || y < 0 || y >= 16)
                    return;

                pixels[(15 - y) * 16 + x] = ink;
            }

            Pixel(3, 8);
            Pixel(4, 9);
            Pixel(5, 10);
            Pixel(6, 10);
            Pixel(7, 9);
            Pixel(8, 8);
            Pixel(9, 7);
            Pixel(10, 6);
            Pixel(11, 5);
            Pixel(12, 4);

            Pixel(3, 9);
            Pixel(4, 10);
            Pixel(5, 11);
            Pixel(6, 11);
            Pixel(7, 10);
            Pixel(8, 9);
            Pixel(9, 8);
            Pixel(10, 7);
            Pixel(11, 6);
            Pixel(12, 5);

            _checkIcon.SetPixels32(pixels);
            _checkIcon.Apply(false, true);
            return _checkIcon;
        }
        private static Texture2D GetGeneratedPasteIcon()
        {
            if (_generatedPasteIcon != null)
                return _generatedPasteIcon;

            Color32 clear = new(0, 0, 0, 0);
            Color32 ink = EditorGUIUtility.isProSkin ? new Color32(190, 190, 190, 255) : new Color32(80, 80, 80, 255);
            _generatedPasteIcon = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = "Looga Generated Paste Icon",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Point
            };

            Color32[] pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = clear;

            void Pixel(int x, int y)
            {
                if (x < 0 || x >= 16 || y < 0 || y >= 16)
                    return;

                pixels[y * 16 + x] = ink;
            }

            for (int x = 4; x <= 11; x++)
            {
                Pixel(x, 3);
                Pixel(x, 12);
            }

            for (int y = 3; y <= 12; y++)
            {
                Pixel(4, y);
                Pixel(11, y);
            }

            for (int x = 6; x <= 9; x++)
            {
                Pixel(x, 13);
                Pixel(x, 14);
            }

            Pixel(5, 13);
            Pixel(10, 13);
            for (int x = 6; x <= 9; x++)
                Pixel(x, 10);
            for (int x = 6; x <= 9; x++)
                Pixel(x, 8);
            for (int x = 6; x <= 8; x++)
                Pixel(x, 6);

            _generatedPasteIcon.SetPixels32(pixels);
            _generatedPasteIcon.Apply(false, true);
            return _generatedPasteIcon;
        }

        private readonly struct HeaderCandidate
        {
            public readonly VisualElement Element;
            public readonly Component Component;
            public readonly Object[] Targets;
            public readonly float Y;
            public readonly bool Preferred;

            public HeaderCandidate(VisualElement element, Component component, Object[] targets, float y, bool preferred)
            {
                Element = element;
                Component = component;
                Targets = targets;
                Y = y;
                Preferred = preferred;
            }
        }
    }
}







