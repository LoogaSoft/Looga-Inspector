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
        private const string PasteButtonName = "looga-component-header-paste-button";
        private const float ButtonSize = 15f;
        private const float ButtonGap = 2f;
        private static readonly Color ButtonIdleColor = new(0f, 0f, 0f, 0f);
        private static readonly Color ButtonHoverColor = new(0.42f, 0.42f, 0.42f, 0.72f);
        private static readonly Color ButtonPressedColor = new(0.30f, 0.30f, 0.30f, 0.9f);
        private const float RightOffset = 68f;
        private const float TopOffset = 4f;

        private static readonly Type InspectorWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
        private static readonly FieldInfo AllInspectorsField = InspectorWindowType?.GetField("m_AllInspectors", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly List<VisualElement> ScratchElements = new();
        private static readonly Dictionary<int, HeaderCandidate> CandidateByComponent = new();
        private static Texture2D _copyIcon;
        private static Texture2D _pasteIcon;

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
            RemoveInjectedContainers(root);

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

            foreach (HeaderCandidate candidate in CandidateByComponent.Values)
                EnsureButtonContainer(candidate.Element, candidate.Component, candidate.Targets);
        }

        private static void RemoveInjectedContainers(VisualElement root)
        {
            ScratchElements.Clear();
            root.Query<VisualElement>(name: ButtonContainerName).ForEach(element => ScratchElements.Add(element));
            for (int i = 0; i < ScratchElements.Count; i++)
                ScratchElements[i].RemoveFromHierarchy();
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
            VisualElement container = new()
            {
                name = ButtonContainerName,
                pickingMode = PickingMode.Position
            };
            container.style.position = Position.Absolute;
            container.style.top = TopOffset;
            container.style.right = RightOffset;
            container.style.width = ButtonSize * 2f + ButtonGap;
            container.style.height = ButtonSize;
            container.style.flexDirection = FlexDirection.Row;

            Button copyButton = CreateHeaderButton(GetCopyIcon(), "Copy component", () => LoogaComponentClipboard.CopyComponent(component));
            Button pasteButton = CreateHeaderButton(GetPasteIcon(), "Paste values into this component", () => LoogaComponentClipboard.PasteValuesIntoComponents(targets));
            pasteButton.name = PasteButtonName;
            pasteButton.style.marginLeft = ButtonGap;

            container.Add(copyButton);
            container.Add(pasteButton);
            editorElement.Add(container);
            UpdatePasteButton(container, targets);
        }

        private static Button CreateHeaderButton(Texture2D icon, string tooltip, Action clicked)
        {
            Button button = new(clicked)
            {
                tooltip = tooltip,
                text = icon == null ? tooltip[..1] : string.Empty
            };
            button.style.width = ButtonSize;
            button.style.height = ButtonSize;
            button.style.paddingLeft = 2f;
            button.style.paddingRight = 2f;
            button.style.paddingTop = 2f;
            button.style.paddingBottom = 2f;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
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
                button.style.backgroundImage = new StyleBackground(icon);
                button.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }

            button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = ButtonHoverColor);
            button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = ButtonIdleColor);
            button.RegisterCallback<MouseDownEvent>(_ => button.style.backgroundColor = ButtonPressedColor);
            button.RegisterCallback<MouseUpEvent>(_ => button.style.backgroundColor = ButtonHoverColor);
            return button;
        }

        private static void UpdatePasteButton(VisualElement container, Object[] targets)
        {
            Button pasteButton = container.Q<Button>(name: PasteButtonName);
            if (pasteButton == null)
                return;

            pasteButton.SetEnabled(LoogaComponentClipboard.CanPasteValuesIntoComponents(targets));
        }

        private static Texture2D GetCopyIcon()
        {
            return _copyIcon ??= EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_TreeEditor.Duplicate" : "TreeEditor.Duplicate").image as Texture2D;
        }

        private static Texture2D GetPasteIcon()
        {
            return _pasteIcon ??= EditorGUIUtility.IconContent(EditorGUIUtility.isProSkin ? "d_Clipboard" : "Clipboard").image as Texture2D;
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
