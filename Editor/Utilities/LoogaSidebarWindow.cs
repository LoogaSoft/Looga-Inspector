using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Base editor workspace with discoverable, feature-owned sidebar pages.
    /// Pages are isolated hidden editor objects, so their normal editor lifecycle remains intact.
    /// </summary>
    public abstract class LoogaSidebarWindow : EditorWindow
    {
        private readonly List<ILoogaSidebarPage> _pages = new();
        private readonly List<EditorWindow> _pageObjects = new();
        private Vector2 _navigationScroll;
        private int _selectedPage;

        protected abstract string WorkspaceId { get; }
        protected virtual string ModeLabel => EditorApplication.isPlaying ? "Play Mode" : "Edit Mode";
        private static readonly Vector2 MinimumWindowSize = new(980f, 520f);

        protected virtual void OnEnable()
        {
            wantsMouseMove = true;
            ApplyMinimumSize();
            EnsureSidebarPages();
        }

        protected virtual void OnDisable()
        {
            DestroySidebarPages();
        }

        protected virtual void OnGUI()
        {
            ApplyMinimumSize();
            EnsureSidebarPages();
            float toolbarHeight = Mathf.Max(
                EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing,
                EditorStyles.toolbar.fixedHeight);
            Rect toolbarRect = new(0f, 0f, position.width, toolbarHeight);
            DrawToolbar(toolbarRect);
            Rect bodyRect = new(
                0f,
                toolbarRect.yMax,
                position.width,
                Mathf.Max(1f, position.height - toolbarRect.yMax));
            float navigationWidth = Mathf.Min(LoogaSidebarGUI.DefaultWidth, bodyRect.width);
            Rect navigationRect = new(bodyRect.x, bodyRect.y, navigationWidth, bodyRect.height);
            Rect dividerRect = new(
                navigationRect.xMax,
                bodyRect.y,
                LoogaSidebarGUI.DividerWidth,
                bodyRect.height);
            Rect contentRect = new(
                dividerRect.xMax,
                bodyRect.y,
                Mathf.Max(1f, bodyRect.xMax - dividerRect.xMax),
                bodyRect.height);

            DrawNavigation(navigationRect);
            LoogaSidebarGUI.Divider(dividerRect);
            DrawSelectedPage(contentRect);
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
            _selectedPage = Mathf.Clamp(_selectedPage, 0, Mathf.Max(0, _pages.Count - 1));
        }

        protected void SelectSidebarPage(string pageId)
        {
            _selectedPage = 0;
            if (string.IsNullOrWhiteSpace(pageId))
                return;

            for (int i = 0; i < _pages.Count; i++)
            {
                if (!string.Equals(_pages[i].PageId, pageId, StringComparison.Ordinal))
                    continue;

                _selectedPage = i;
                return;
            }
        }

        protected void CenterOnMainEditorWindow(Vector2 minimumSize)
        {
            minSize = Vector2.Max(MinimumWindowSize, minimumSize);
            Rect mainWindow = EditorGUIUtility.GetMainWindowPosition();
            Rect current = position;
            current.width = Mathf.Max(current.width, minSize.x);
            current.height = Mathf.Max(current.height, minSize.y);
            current.x = mainWindow.x + (mainWindow.width - current.width) * 0.5f;
            current.y = mainWindow.y + (mainWindow.height - current.height) * 0.5f;
            position = current;
        }

        private void DrawToolbar(Rect toolbarRect)
        {
            GUILayout.BeginArea(toolbarRect);
            try
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandHeight(true)))
                {
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                        RefreshSelectedPage();

                    GUILayout.FlexibleSpace();
                    GUILayout.Label(ModeLabel, EditorStyles.miniLabel);
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void DrawNavigation(Rect navigationRect)
        {
            _selectedPage = LoogaSidebarGUI.Navigation(
                navigationRect,
                _navigationScroll,
                _selectedPage,
                _pages.Count,
                index => _pages[index].DisplayName,
                out _navigationScroll);
        }

        private void DrawSelectedPage(Rect contentRect)
        {
            GUILayout.BeginArea(contentRect);
            try
            {
                using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)))
                {
                    GUILayout.Space(LoogaSidebarGUI.ContentPadding);
                    if (_pages.Count == 0)
                    {
                        EditorGUILayout.LabelField("No Pages", LoogaSidebarGUI.HeaderStyle);
                        GUILayout.Space(8f);
                        EditorGUILayout.HelpBox("No sidebar pages are registered for this workspace.", MessageType.Info);
                        return;
                    }

                    int index = Mathf.Clamp(_selectedPage, 0, _pages.Count - 1);
                    ILoogaSidebarPage page = _pages[index];
                    EditorGUILayout.LabelField(page.DisplayName, LoogaSidebarGUI.HeaderStyle);
                    GUILayout.Space(2f);
                    EditorGUILayout.LabelField(page.Description, EditorStyles.wordWrappedMiniLabel);
                    GUILayout.Space(8f);
                    page.DrawPage();
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void ApplyMinimumSize()
        {
            Vector2 minimum = MinimumWindowSize;
            if (minSize.x < minimum.x || minSize.y < minimum.y)
                minSize = Vector2.Max(minSize, minimum);
        }

        private void RefreshSelectedPage()
        {
            if (_pages.Count > 0)
                _pages[Mathf.Clamp(_selectedPage, 0, _pages.Count - 1)].RefreshPage();

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
