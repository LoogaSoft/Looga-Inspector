using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Base editor workspace with an overview and discoverable, feature-owned sidebar pages.
    /// Pages are isolated hidden editor objects, so their normal editor lifecycle remains intact.
    /// </summary>
    public abstract class LoogaSidebarWindow : EditorWindow
    {
        public const string OverviewPageId = "overview";

        private readonly List<ILoogaSidebarPage> _pages = new();
        private readonly List<EditorWindow> _pageObjects = new();
        private Vector2 _navigationScroll;
        private Vector2 _overviewScroll;
        private int _selectedPage;

        protected abstract string WorkspaceId { get; }
        protected virtual string OverviewDescription => "Open a workspace page from the navigation on the left.";
        protected virtual string ModeLabel => EditorApplication.isPlaying ? "Play Mode" : "Edit Mode";

        protected virtual void OnEnable()
        {
            wantsMouseMove = true;
            EnsureSidebarPages();
        }

        protected virtual void OnDisable()
        {
            DestroySidebarPages();
        }

        protected virtual void OnGUI()
        {
            EnsureSidebarPages();
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawNavigation();
                Rect divider = GUILayoutUtility.GetRect(
                    LoogaSidebarGUI.DividerWidth,
                    0f,
                    GUILayout.Width(LoogaSidebarGUI.DividerWidth),
                    GUILayout.ExpandHeight(true));
                LoogaSidebarGUI.Divider(divider);
                DrawSelectedPage();
            }
        }

        protected void EnsureSidebarPages()
        {
            if (_pages.Count > 0)
                return;

            foreach (Type type in TypeCache.GetTypesDerivedFrom<ILoogaSidebarPage>())
            {
                LoogaSidebarPageAttribute attribute =
                    Attribute.GetCustomAttribute(type, typeof(LoogaSidebarPageAttribute)) as LoogaSidebarPageAttribute;
                if (type.IsAbstract || attribute == null ||
                    !string.Equals(attribute.WorkspaceId, WorkspaceId, StringComparison.Ordinal) ||
                    !typeof(EditorWindow).IsAssignableFrom(type))
                {
                    continue;
                }

                EditorWindow pageObject = CreateInstance(type) as EditorWindow;
                if (pageObject is not ILoogaSidebarPage page)
                    continue;

                pageObject.hideFlags = HideFlags.HideAndDontSave;
                page.Attach(this);
                _pageObjects.Add(pageObject);
                _pages.Add(page);
            }

            _pages.Sort(ComparePages);
            _selectedPage = Mathf.Clamp(_selectedPage, 0, _pages.Count);
        }

        protected void SelectSidebarPage(string pageId)
        {
            _selectedPage = 0;
            if (string.IsNullOrWhiteSpace(pageId) || pageId == OverviewPageId)
                return;

            for (int i = 0; i < _pages.Count; i++)
            {
                if (!string.Equals(_pages[i].PageId, pageId, StringComparison.Ordinal))
                    continue;

                _selectedPage = i + 1;
                return;
            }
        }

        protected void CenterOnMainEditorWindow(Vector2 minimumSize)
        {
            Rect mainWindow = EditorGUIUtility.GetMainWindowPosition();
            Rect current = position;
            current.width = Mathf.Max(current.width, minimumSize.x);
            current.height = Mathf.Max(current.height, minimumSize.y);
            current.x = mainWindow.x + (mainWindow.width - current.width) * 0.5f;
            current.y = mainWindow.y + (mainWindow.height - current.height) * 0.5f;
            position = current;
        }

        protected virtual void DrawOverview()
        {
            _overviewScroll = EditorGUILayout.BeginScrollView(_overviewScroll);
            EditorGUILayout.LabelField(OverviewDescription, EditorStyles.wordWrappedLabel);
            GUILayout.Space(10f);

            for (int i = 0; i < _pages.Count; i++)
            {
                ILoogaSidebarPage page = _pages[i];
                LoogaGUILayout.BoxSmall(page.DisplayName, () =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(page.Description, EditorStyles.wordWrappedMiniLabel);
                        GUILayout.Space(8f);
                        if (GUILayout.Button("Open", EditorStyles.miniButton, GUILayout.Width(64f)))
                            _selectedPage = i + 1;
                    }
                });
                GUILayout.Space(5f);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                    RefreshSelectedPage();

                GUILayout.FlexibleSpace();
                GUILayout.Label(ModeLabel, EditorStyles.miniLabel);
            }
        }

        private void DrawNavigation()
        {
            Rect navigationRect = GUILayoutUtility.GetRect(
                LoogaSidebarGUI.DefaultWidth,
                0f,
                GUILayout.Width(LoogaSidebarGUI.DefaultWidth),
                GUILayout.ExpandHeight(true));

            _selectedPage = LoogaSidebarGUI.Navigation(
                navigationRect,
                _navigationScroll,
                _selectedPage,
                _pages.Count + 1,
                index => index == 0 ? "Overview" : _pages[index - 1].DisplayName,
                out _navigationScroll);
        }

        private void DrawSelectedPage()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
            {
                GUILayout.Space(LoogaSidebarGUI.ContentPadding);
                if (_selectedPage <= 0 || _pages.Count == 0)
                {
                    EditorGUILayout.LabelField("Overview", LoogaSidebarGUI.HeaderStyle);
                    GUILayout.Space(8f);
                    DrawOverview();
                    return;
                }

                int index = Mathf.Clamp(_selectedPage - 1, 0, _pages.Count - 1);
                ILoogaSidebarPage page = _pages[index];
                EditorGUILayout.LabelField(page.DisplayName, LoogaSidebarGUI.HeaderStyle);
                GUILayout.Space(2f);
                EditorGUILayout.LabelField(page.Description, EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(8f);
                page.DrawPage();
            }
        }

        private void RefreshSelectedPage()
        {
            if (_selectedPage <= 0)
            {
                for (int i = 0; i < _pages.Count; i++)
                    _pages[i].RefreshPage();
            }
            else
            {
                _pages[Mathf.Clamp(_selectedPage - 1, 0, _pages.Count - 1)].RefreshPage();
            }

            Repaint();
        }

        private void DestroySidebarPages()
        {
            for (int i = 0; i < _pageObjects.Count; i++)
            {
                if (_pageObjects[i] != null)
                    DestroyImmediate(_pageObjects[i]);
            }

            _pageObjects.Clear();
            _pages.Clear();
        }

        private static int ComparePages(ILoogaSidebarPage left, ILoogaSidebarPage right)
        {
            int order = left.Order.CompareTo(right.Order);
            return order != 0
                ? order
                : string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        }
    }
}
