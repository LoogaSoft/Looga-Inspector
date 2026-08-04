using System;
using System.Collections.Generic;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Draws an attributed serialized object as an expandable sidebar of asset pages.
    /// Section fields identify domain roots. ScriptableObject references inside each root become child pages.
    /// </summary>
    public sealed class LoogaSidebarSerializedView : IDisposable
    {
        private static readonly Dictionary<Type, Section[]> SectionCache = new();

        private readonly HashSet<string> _expandedSections = new(StringComparer.Ordinal);
        private readonly Dictionary<Object, SerializedObject> _serializedObjectCache = new();
        private Vector2 _navigationScroll;
        private Vector2 _contentScroll;
        private string _selectedPageId = string.Empty;
        private int _rootInstanceId;
        private UnityEditor.Editor _pageEditor;
        private Object _pageEditorTarget;

        public bool Draw(SerializedObject serializedObject, float height = 240f)
        {
            if (serializedObject?.targetObject == null)
                return false;

            Section[] sections = GetSections(serializedObject.targetObject.GetType());
            if (sections.Length == 0)
                return false;

            EnsureRootState(serializedObject.targetObject, sections);

            List<Page> pages = new();
            List<LoogaSidebarGUI.AccordionGroup> groups = BuildNavigation(serializedObject, sections, pages);
            EnsureInitialState(pages);

            height = Mathf.Max(1f, height);
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(height)))
            {
                Rect navigationRect = GUILayoutUtility.GetRect(
                    LoogaSidebarGUI.DefaultWidth,
                    LoogaSidebarGUI.DefaultWidth,
                    height,
                    height,
                    GUILayout.Width(LoogaSidebarGUI.DefaultWidth),
                    GUILayout.Height(height));

                LoogaSidebarGUI.AccordionNavigation(
                    navigationRect,
                    _navigationScroll,
                    groups,
                    _selectedPageId,
                    out _navigationScroll,
                    out string nextPageId,
                    out string toggledSectionId);

                if (!string.IsNullOrEmpty(toggledSectionId))
                {
                    if (!_expandedSections.Add(toggledSectionId))
                        _expandedSections.Remove(toggledSectionId);
                }

                if (!string.Equals(nextPageId, _selectedPageId, StringComparison.Ordinal))
                {
                    _selectedPageId = nextPageId;
                    _contentScroll = Vector2.zero;
                    ReleasePageEditor();
                }

                Rect dividerRect = GUILayoutUtility.GetRect(
                    LoogaSidebarGUI.DividerWidth,
                    LoogaSidebarGUI.DividerWidth,
                    height,
                    height,
                    GUILayout.Width(LoogaSidebarGUI.DividerWidth),
                    GUILayout.Height(height));
                LoogaSidebarGUI.Divider(dividerRect);

                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.Height(height)))
                    DrawSelectedPage(pages);
            }

            return true;
        }

        public void Dispose()
        {
            ReleasePageEditor();
            _serializedObjectCache.Clear();
            _expandedSections.Clear();
            _selectedPageId = string.Empty;
            _rootInstanceId = 0;
        }

        public static bool Supports(Type type)
        {
            return type != null &&
                   type.IsDefined(typeof(SidebarLayoutAttribute), true) &&
                   GetSections(type).Length > 0;
        }

        private List<LoogaSidebarGUI.AccordionGroup> BuildNavigation(
            SerializedObject root,
            IReadOnlyList<Section> sections,
            List<Page> pages)
        {
            List<LoogaSidebarGUI.AccordionGroup> groups = new(sections.Count);
            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                Section section = sections[sectionIndex];
                List<LoogaSidebarGUI.AccordionItem> items = new();

                for (int propertyIndex = 0; propertyIndex < section.PropertyNames.Length; propertyIndex++)
                {
                    SerializedProperty rootProperty = root.FindProperty(section.PropertyNames[propertyIndex]);
                    if (rootProperty == null)
                        continue;

                    if (rootProperty.objectReferenceValue is ScriptableObject domain &&
                        AddDomainPages(section, rootProperty, domain, pages, items))
                    {
                        continue;
                    }

                    AddPage(
                        section,
                        root,
                        rootProperty,
                        ResolveReferenceType(root.targetObject.GetType(), rootProperty),
                        pages,
                        items);
                }

                groups.Add(new LoogaSidebarGUI.AccordionGroup(
                    section.Name,
                    section.Name,
                    _expandedSections.Contains(section.Name),
                    items));
            }

            return groups;
        }

        private bool AddDomainPages(
            Section section,
            SerializedProperty rootProperty,
            ScriptableObject domain,
            List<Page> pages,
            List<LoogaSidebarGUI.AccordionItem> items)
        {
            SerializedObject domainObject = GetSerializedObject(domain);
            domainObject.UpdateIfRequiredOrScript();
            SerializedProperty iterator = domainObject.GetIterator();
            bool enterChildren = true;
            bool addedPage = false;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyPath == "m_Script" || iterator.propertyType != SerializedPropertyType.ObjectReference)
                    continue;

                Type referenceType = ResolveReferenceType(domain.GetType(), iterator);
                if (referenceType == null || !typeof(ScriptableObject).IsAssignableFrom(referenceType))
                    continue;

                AddPage(section, domainObject, iterator, referenceType, pages, items, rootProperty.propertyPath);
                addedPage = true;
            }

            return addedPage;
        }

        private static void AddPage(
            Section section,
            SerializedObject owner,
            SerializedProperty property,
            Type referenceType,
            List<Page> pages,
            List<LoogaSidebarGUI.AccordionItem> items,
            string parentPath = "")
        {
            string id = string.IsNullOrEmpty(parentPath)
                ? $"{section.Name}/{property.propertyPath}"
                : $"{section.Name}/{parentPath}/{property.propertyPath}";
            Page page = new(id, property.displayName, owner, property.propertyPath, referenceType);
            pages.Add(page);
            items.Add(new LoogaSidebarGUI.AccordionItem(page.Id, page.DisplayName));
        }

        private void EnsureRootState(Object root, IReadOnlyList<Section> sections)
        {
            int instanceId = root.GetInstanceID();
            if (_rootInstanceId == instanceId)
                return;

            ReleasePageEditor();
            _serializedObjectCache.Clear();
            _expandedSections.Clear();
            _selectedPageId = string.Empty;
            _navigationScroll = Vector2.zero;
            _contentScroll = Vector2.zero;
            _rootInstanceId = instanceId;

            // Open the first group once so a new workspace has an immediate starting point.
            _expandedSections.Add(sections[0].Name);
        }

        private void EnsureInitialState(IReadOnlyList<Page> pages)
        {
            for (int i = 0; i < pages.Count; i++)
            {
                if (string.Equals(pages[i].Id, _selectedPageId, StringComparison.Ordinal))
                    return;
            }

            _selectedPageId = pages.Count > 0 ? pages[0].Id : string.Empty;
            _contentScroll = Vector2.zero;
            ReleasePageEditor();
        }

        private void DrawSelectedPage(IReadOnlyList<Page> pages)
        {
            Page selectedPage = null;
            for (int i = 0; i < pages.Count; i++)
            {
                if (!string.Equals(pages[i].Id, _selectedPageId, StringComparison.Ordinal))
                    continue;

                selectedPage = pages[i];
                break;
            }

            GUILayout.Space(LoogaSidebarGUI.ContentPadding);
            if (selectedPage == null)
            {
                EditorGUILayout.LabelField("Configuration", LoogaSidebarGUI.HeaderStyle);
                GUILayout.Space(8f);
                EditorGUILayout.HelpBox("Select a configuration asset from the sidebar.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(selectedPage.DisplayName, LoogaSidebarGUI.HeaderStyle);
            GUILayout.Space(4f);
            DrawReferenceField(selectedPage);
            GUILayout.Space(6f);

            _contentScroll = EditorGUILayout.BeginScrollView(_contentScroll, GUILayout.ExpandHeight(true));
            try
            {
                Object target = selectedPage.Target;
                if (target == null)
                {
                    EditorGUILayout.HelpBox(
                        $"Assign a {ObjectNames.NicifyVariableName(selectedPage.ReferenceType.Name)} asset to edit it here.",
                        MessageType.Info);
                    ReleasePageEditor();
                    return;
                }

                EnsurePageEditor(target);
                if (_pageEditor != null)
                {
                    _pageEditor.OnInspectorGUI();
                }
                else
                {
                    EditorGUILayout.HelpBox("Unity could not create an editor for this asset.", MessageType.Warning);
                }
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawReferenceField(Page page)
        {
            page.Owner.UpdateIfRequiredOrScript();
            SerializedProperty property = page.Owner.FindProperty(page.PropertyPath);
            if (property == null)
                return;

            EditorGUI.BeginChangeCheck();
            Object next = EditorGUILayout.ObjectField("Asset", property.objectReferenceValue, page.ReferenceType, false);
            if (!EditorGUI.EndChangeCheck())
                return;

            property.objectReferenceValue = next;
            page.Owner.ApplyModifiedProperties();
            _contentScroll = Vector2.zero;
            ReleasePageEditor();
        }

        private void EnsurePageEditor(Object target)
        {
            if (_pageEditorTarget == target && _pageEditor != null)
                return;

            ReleasePageEditor();
            UnityEditor.Editor.CreateCachedEditor(target, null, ref _pageEditor);
            _pageEditorTarget = target;
        }

        private void ReleasePageEditor()
        {
            if (_pageEditor != null)
                Object.DestroyImmediate(_pageEditor);

            _pageEditor = null;
            _pageEditorTarget = null;
        }

        private SerializedObject GetSerializedObject(Object target)
        {
            if (_serializedObjectCache.TryGetValue(target, out SerializedObject cached) && cached.targetObject != null)
                return cached;

            SerializedObject serializedObject = new(target);
            _serializedObjectCache[target] = serializedObject;
            return serializedObject;
        }

        private static Type ResolveReferenceType(Type ownerType, SerializedProperty property)
        {
            FieldInfo field = FindField(ownerType, property.name);
            if (field != null && typeof(Object).IsAssignableFrom(field.FieldType))
                return field.FieldType;

            return property.objectReferenceValue != null
                ? property.objectReferenceValue.GetType()
                : typeof(ScriptableObject);
        }

        private static FieldInfo FindField(Type type, string fieldName)
        {
            for (Type current = type; current != null && current != typeof(Object); current = current.BaseType)
            {
                FieldInfo field = current.GetField(
                    fieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null)
                    return field;
            }

            return null;
        }

        private static Section[] GetSections(Type type)
        {
            if (SectionCache.TryGetValue(type, out Section[] cached))
                return cached;

            Dictionary<string, SectionBuilder> builders = new(StringComparer.Ordinal);
            for (Type current = type; current != null && current != typeof(Object); current = current.BaseType)
            {
                FieldInfo[] fields = current.GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                for (int i = 0; i < fields.Length; i++)
                {
                    SidebarSectionAttribute attribute = fields[i].GetCustomAttribute<SidebarSectionAttribute>();
                    if (attribute == null || string.IsNullOrWhiteSpace(attribute.Name))
                        continue;

                    if (!builders.TryGetValue(attribute.Name, out SectionBuilder builder))
                    {
                        builder = new SectionBuilder(attribute.Name, attribute.Order);
                        builders.Add(attribute.Name, builder);
                    }

                    builder.PropertyNames.Add(fields[i].Name);
                }
            }

            List<Section> sections = new(builders.Count);
            foreach (SectionBuilder builder in builders.Values)
                sections.Add(new Section(builder.Name, builder.Order, builder.PropertyNames.ToArray()));

            sections.Sort(CompareSections);
            cached = sections.ToArray();
            SectionCache[type] = cached;
            return cached;
        }

        private static int CompareSections(Section left, Section right)
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0 ? order : string.Compare(left.Name, right.Name, StringComparison.Ordinal);
        }

        private sealed class Page
        {
            public Page(string id, string displayName, SerializedObject owner, string propertyPath, Type referenceType)
            {
                Id = id;
                DisplayName = displayName;
                Owner = owner;
                PropertyPath = propertyPath;
                ReferenceType = referenceType;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public SerializedObject Owner { get; }
            public string PropertyPath { get; }
            public Type ReferenceType { get; }
            public Object Target => Owner.FindProperty(PropertyPath)?.objectReferenceValue;
        }

        private sealed class SectionBuilder
        {
            public SectionBuilder(string name, int order)
            {
                Name = name;
                Order = order;
            }

            public string Name { get; }
            public int Order { get; }
            public List<string> PropertyNames { get; } = new();
        }

        private readonly struct Section
        {
            public Section(string name, int order, string[] propertyNames)
            {
                Name = name;
                Order = order;
                PropertyNames = propertyNames;
            }

            public string Name { get; }
            public int Order { get; }
            public string[] PropertyNames { get; }
        }
    }
}
