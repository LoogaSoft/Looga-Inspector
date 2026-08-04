using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Presents optional Looga package integrations in one workspace. Providers remain in their
    /// owning packages and are discovered through a small reflection contract.
    /// </summary>
    internal sealed class LoogaPackageSupportWindow : EditorWindow
    {
        private const string MenuPath = "LoogaSoft/Package Support";
        private const float ToolbarHeight = 21f;
        private const float CardHeight = 82f;
        private const float ContentPadding = 12f;

        private readonly List<PackageSupportPage> _pages = new();
        private GUIStyle _availableStatusStyle;
        private GUIStyle _enabledStatusStyle;
        private GUIStyle _unavailableStatusStyle;
        private Vector2 _navigationScroll;
        private Vector2 _contentScroll;
        private int _selectedPage;

        [MenuItem(MenuPath, priority = 0)]
        private static void Open()
        {
            LoogaPackageSupportWindow window = GetWindow<LoogaPackageSupportWindow>();
            window.titleContent = new GUIContent("Looga Package Support");
            window.minSize = new Vector2(720f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            RefreshProviders();
        }

        private void OnGUI()
        {
            DrawToolbar();

            Rect bodyRect = new(
                0f,
                ToolbarHeight,
                position.width,
                Mathf.Max(1f, position.height - ToolbarHeight));
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
            DrawContent(contentRect);

            if (Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private void DrawToolbar()
        {
            Rect toolbarRect = new(0f, 0f, position.width, ToolbarHeight);
            GUILayout.BeginArea(toolbarRect);
            try
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandHeight(true)))
                {
                    if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
                        RefreshProviders();

                    GUILayout.FlexibleSpace();
                    GUILayout.Label($"{_pages.Count} package(s)", EditorStyles.miniLabel);
                }
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void DrawNavigation(Rect rect)
        {
            _selectedPage = LoogaSidebarGUI.Navigation(
                rect,
                _navigationScroll,
                _selectedPage,
                _pages.Count,
                index => _pages[index].Name,
                out _navigationScroll);
        }

        private void DrawContent(Rect rect)
        {
            GUILayout.BeginArea(rect);
            try
            {
                if (_pages.Count == 0)
                {
                    GUILayout.Space(ContentPadding);
                    EditorGUILayout.LabelField("Package Support", LoogaSidebarGUI.HeaderStyle);
                    GUILayout.Space(6f);
                    EditorGUILayout.HelpBox("No optional Looga package integrations were found.", MessageType.Info);
                    return;
                }

                PackageSupportPage page = _pages[Mathf.Clamp(_selectedPage, 0, _pages.Count - 1)];
                _contentScroll = EditorGUILayout.BeginScrollView(_contentScroll);
                GUILayout.Space(ContentPadding);
                EditorGUILayout.LabelField(page.Name, LoogaSidebarGUI.HeaderStyle);
                GUILayout.Space(2f);
                EditorGUILayout.LabelField(
                    "Enable only the integrations used by this project.",
                    EditorStyles.wordWrappedMiniLabel);
                GUILayout.Space(10f);

                for (int i = 0; i < page.Providers.Count; i++)
                {
                    DrawProvider(page.Providers[i]);
                    GUILayout.Space(4f);
                }

                GUILayout.Space(ContentPadding);
                EditorGUILayout.EndScrollView();
            }
            finally
            {
                GUILayout.EndArea();
            }
        }

        private void DrawProvider(OptionalSupportProvider provider)
        {
            Rect cardRect = LoogaEditorStyle.PixelSnap(
                GUILayoutUtility.GetRect(1f, CardHeight, GUILayout.ExpandWidth(true)));
            EditorGUI.DrawRect(cardRect, LoogaEditorStyle.AlternateBoxColor);

            Rect innerRect = new(
                cardRect.x + 12f,
                cardRect.y + 8f,
                cardRect.width - 24f,
                cardRect.height - 16f);
            bool enabled = provider.Enabled;
            bool available = provider.Available;

            Rect titleRect = new(innerRect.x, innerRect.y, innerRect.width - 100f, 20f);
            EditorGUI.LabelField(titleRect, provider.IntegrationName, EditorStyles.boldLabel);

            Rect toggleRect = new(innerRect.xMax - 88f, innerRect.y, 88f, 20f);
            using (new EditorGUI.DisabledScope(!available && !enabled))
            {
                bool nextEnabled = EditorGUI.ToggleLeft(toggleRect, "Enabled", enabled);
                if (nextEnabled != enabled)
                    SetProviderEnabled(provider, nextEnabled);
            }

            string status = enabled ? "Enabled" : available ? "Available" : "Unavailable";
            Rect statusRect = new(innerRect.x, innerRect.y + 22f, innerRect.width, 18f);
            EditorGUI.LabelField(statusRect, status, GetStatusStyle(enabled, available));

            string detail = available ? provider.Description : provider.UnavailableReason;
            Rect detailRect = new(innerRect.x, innerRect.y + 41f, innerRect.width, 28f);
            EditorGUI.LabelField(detailRect, detail, EditorStyles.wordWrappedMiniLabel);
        }

        private GUIStyle GetStatusStyle(bool enabled, bool available)
        {
            _enabledStatusStyle ??= CreateStatusStyle(new Color(0.45f, 0.78f, 0.48f));
            _availableStatusStyle ??= CreateStatusStyle(LoogaEditorStyle.TextColor);
            _unavailableStatusStyle ??= CreateStatusStyle(new Color(0.88f, 0.58f, 0.30f));

            return enabled
                ? _enabledStatusStyle
                : available ? _availableStatusStyle : _unavailableStatusStyle;
        }

        private static GUIStyle CreateStatusStyle(Color textColor)
        {
            GUIStyle style = new(EditorStyles.miniLabel);
            style.normal.textColor = textColor;
            return style;
        }

        private void SetProviderEnabled(OptionalSupportProvider provider, bool enabled)
        {
            try
            {
                provider.SetEnabled(enabled);
                provider.RefreshState();
                Repaint();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Integration Update Failed",
                    $"Looga could not update {provider.IntegrationName} support.\n\n{exception.GetBaseException().Message}",
                    "OK");
            }
        }

        private void RefreshProviders()
        {
            string selectedName = _pages.Count > 0 && _selectedPage < _pages.Count
                ? _pages[_selectedPage].Name
                : string.Empty;

            _pages.Clear();
            IEnumerable<OptionalSupportProvider> providers = OptionalSupportProvider.Discover();
            foreach (IGrouping<string, OptionalSupportProvider> group in providers.GroupBy(provider => provider.PackageName))
            {
                _pages.Add(new PackageSupportPage(
                    group.Key,
                    group.OrderBy(provider => provider.IntegrationName, StringComparer.Ordinal).ToList()));
            }

            _pages.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
            _selectedPage = Mathf.Clamp(
                _pages.FindIndex(page => string.Equals(page.Name, selectedName, StringComparison.Ordinal)),
                0,
                Mathf.Max(0, _pages.Count - 1));
            Repaint();
        }

        private sealed class PackageSupportPage
        {
            public PackageSupportPage(string name, List<OptionalSupportProvider> providers)
            {
                Name = name;
                Providers = providers;
            }

            public string Name { get; }
            public List<OptionalSupportProvider> Providers { get; }
        }

        private sealed class OptionalSupportProvider
        {
            private const BindingFlags StaticPublic = BindingFlags.Static | BindingFlags.Public;
            private readonly MethodInfo _getUnavailableReason;
            private readonly MethodInfo _isEnabled;
            private readonly MethodInfo _setEnabled;

            private OptionalSupportProvider(Type type)
            {
                ProviderId = ReadString(type, "ProviderId");
                PackageName = ReadString(type, "PackageName");
                IntegrationName = ReadString(type, "IntegrationName");
                Description = ReadString(type, "Description");
                _isEnabled = type.GetMethod("IsEnabled", StaticPublic);
                _getUnavailableReason = type.GetMethod("GetUnavailableReason", StaticPublic);
                _setEnabled = type.GetMethod("SetEnabled", StaticPublic, null, new[] { typeof(bool) }, null);
                RefreshState();
            }

            public string ProviderId { get; }
            public string PackageName { get; }
            public string IntegrationName { get; }
            public string Description { get; }
            public bool Enabled { get; private set; }
            public string UnavailableReason { get; private set; }
            public bool Available => string.IsNullOrEmpty(UnavailableReason);

            public void SetEnabled(bool enabled) => _setEnabled.Invoke(null, new object[] { enabled });

            /// <summary>
            /// Refreshes checks that can scan packages or compiled assemblies. Do not call this from OnGUI.
            /// </summary>
            public void RefreshState()
            {
                Enabled = (bool)_isEnabled.Invoke(null, null);
                UnavailableReason = (string)_getUnavailableReason.Invoke(null, null) ?? string.Empty;
            }

            public static IEnumerable<OptionalSupportProvider> Discover()
            {
                List<OptionalSupportProvider> providers = new();
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type type in GetLoadableTypes(assembly))
                    {
                        if (!HasProviderContract(type))
                            continue;

                        providers.Add(new OptionalSupportProvider(type));
                    }
                }

                return providers
                    .Where(provider => !string.IsNullOrWhiteSpace(provider.ProviderId))
                    .GroupBy(provider => provider.ProviderId, StringComparer.Ordinal)
                    .Select(group => group.First());
            }

            private static bool HasProviderContract(Type type)
            {
                return type.GetProperty("ProviderId", StaticPublic)?.PropertyType == typeof(string) &&
                       type.GetProperty("PackageName", StaticPublic)?.PropertyType == typeof(string) &&
                       type.GetProperty("IntegrationName", StaticPublic)?.PropertyType == typeof(string) &&
                       type.GetProperty("Description", StaticPublic)?.PropertyType == typeof(string) &&
                       type.GetMethod("IsEnabled", StaticPublic)?.ReturnType == typeof(bool) &&
                       type.GetMethod("GetUnavailableReason", StaticPublic)?.ReturnType == typeof(string) &&
                       type.GetMethod("SetEnabled", StaticPublic, null, new[] { typeof(bool) }, null) != null;
            }

            private static string ReadString(Type type, string propertyName)
            {
                return type.GetProperty(propertyName, StaticPublic)?.GetValue(null) as string ?? string.Empty;
            }

            private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException exception)
                {
                    return exception.Types.Where(type => type != null);
                }
            }
        }
    }
}
