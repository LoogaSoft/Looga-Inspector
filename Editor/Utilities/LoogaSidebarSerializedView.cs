using System;
using System.Collections.Generic;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Draws an attributed serialized object as an ordered sidebar without requiring a custom editor.
    /// Reflection metadata is cached per inspected type; serialized values remain owned by Unity.
    /// </summary>
    public sealed class LoogaSidebarSerializedView
    {
        private static readonly Dictionary<Type, Section[]> SectionCache = new();

        private Vector2 _navigationScroll;
        private Vector2 _contentScroll;
        private int _selectedSection;

        public bool Draw(SerializedObject serializedObject, float height = 240f)
        {
            if (serializedObject?.targetObject == null)
                return false;

            Section[] sections = GetSections(serializedObject.targetObject.GetType());
            if (sections.Length == 0)
                return false;

            _selectedSection = Mathf.Clamp(_selectedSection, 0, sections.Length - 1);
            height = Mathf.Max(1f, height);
            Rect workspaceRect = GUILayoutUtility.GetRect(
                1f,
                height,
                GUILayout.ExpandWidth(true),
                GUILayout.Height(height));
            float navigationWidth = Mathf.Min(LoogaSidebarGUI.DefaultWidth, workspaceRect.width);
            Rect navigationRect = new(workspaceRect.x, workspaceRect.y, navigationWidth, workspaceRect.height);
            Rect dividerRect = new(
                navigationRect.xMax,
                workspaceRect.y,
                LoogaSidebarGUI.DividerWidth,
                workspaceRect.height);
            Rect contentRect = new(
                dividerRect.xMax,
                workspaceRect.y,
                Mathf.Max(1f, workspaceRect.xMax - dividerRect.xMax),
                workspaceRect.height);

            int previousSelection = _selectedSection;
            _selectedSection = LoogaSidebarGUI.Navigation(
                navigationRect,
                _navigationScroll,
                _selectedSection,
                sections.Length,
                index => sections[index].Name,
                out _navigationScroll);

            if (_selectedSection != previousSelection)
                _contentScroll = Vector2.zero;

            LoogaSidebarGUI.Divider(dividerRect);
            DrawSection(serializedObject, sections[_selectedSection], contentRect);

            return true;
        }

        public static bool Supports(Type type)
        {
            return type != null &&
                   type.IsDefined(typeof(SidebarLayoutAttribute), true) &&
                   GetSections(type).Length > 0;
        }

        private void DrawSection(SerializedObject serializedObject, Section section, Rect contentRect)
        {
            GUILayout.BeginArea(contentRect);
            try
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Space(LoogaSidebarGUI.ContentPadding);
                    EditorGUILayout.LabelField(section.Name, LoogaSidebarGUI.HeaderStyle);
                    GUILayout.Space(8f);

                    float previousLabelWidth = EditorGUIUtility.labelWidth;
                    float availableWidth = Mathf.Max(
                        120f,
                        contentRect.width - (LoogaSidebarGUI.ContentPadding * 2f));
                    EditorGUIUtility.labelWidth = availableWidth * 0.5f;

                    _contentScroll = EditorGUILayout.BeginScrollView(
                        _contentScroll,
                        GUILayout.ExpandWidth(true),
                        GUILayout.ExpandHeight(true));
                    for (int i = 0; i < section.PropertyNames.Length; i++)
                    {
                        SerializedProperty property = serializedObject.FindProperty(section.PropertyNames[i]);
                        if (property == null)
                            continue;

                        EditorGUILayout.PropertyField(property, includeChildren: true);
                        EditorGUILayout.Space(4f);
                    }

                    GUILayout.Space(LoogaSidebarGUI.ContentPadding);
                    EditorGUILayout.EndScrollView();
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private static Section[] GetSections(Type type)
        {
            if (SectionCache.TryGetValue(type, out Section[] cached))
                return cached;

            Dictionary<string, SectionBuilder> builders = new(StringComparer.Ordinal);
            for (Type current = type; current != null && current != typeof(UnityEngine.Object); current = current.BaseType)
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
