using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaComponentHeaderClipboardButtons
    {
        private const float ButtonSize = 18f;
        private const float ButtonGap = 2f;
        private const float BuiltInIconAreaWidth = 66f;

        private static GUIStyle _buttonStyle;
        private static GUIContent _copyIcon;
        private static GUIContent _pasteIcon;

        // Unity does not expose a reliable public insertion point for the built-in component header icon cluster.
        // Keep this utility dormant until we commit to an internal UIElements header injection path.
        private static void InitializeDisabled()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= DrawButtons;
            UnityEditor.Editor.finishedDefaultHeaderGUI += DrawButtons;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
        }

        private static void Dispose()
        {
            UnityEditor.Editor.finishedDefaultHeaderGUI -= DrawButtons;
            AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
        }

        private static void DrawButtons(UnityEditor.Editor editor)
        {
            if (editor == null || editor.target is not Component component)
                return;

            EnsureStyles();

            Rect anchorRect = GUILayoutUtility.GetRect(0f, 0f, GUILayout.ExpandWidth(true));
            Rect headerRect = new(0f, anchorRect.y - EditorGUIUtility.singleLineHeight - 3f, EditorGUIUtility.currentViewWidth, EditorGUIUtility.singleLineHeight + 4f);

            float totalWidth = ButtonSize * 2f + ButtonGap;
            float x = headerRect.xMax - BuiltInIconAreaWidth - totalWidth;
            float y = headerRect.y + Mathf.Floor((headerRect.height - ButtonSize) * 0.5f);
            Rect copyRect = LoogaEditorStyle.PixelSnap(new Rect(x, y, ButtonSize, ButtonSize));
            Rect pasteRect = LoogaEditorStyle.PixelSnap(new Rect(copyRect.xMax + ButtonGap, y, ButtonSize, ButtonSize));

            RequestMouseMoveRepaint(headerRect);

            if (GUI.Button(copyRect, _copyIcon, _buttonStyle))
                LoogaComponentClipboard.CopyComponent(component);

            bool canPaste = LoogaComponentClipboard.CanPasteValuesIntoComponents(editor.targets);
            using (new EditorGUI.DisabledScope(!canPaste))
            {
                if (GUI.Button(pasteRect, _pasteIcon, _buttonStyle))
                    LoogaComponentClipboard.PasteValuesIntoComponents(editor.targets);
            }
        }

        private static void RequestMouseMoveRepaint(Rect rect)
        {
            Event current = Event.current;
            if (!rect.Contains(current.mousePosition))
                return;

            EditorWindow window = EditorWindow.mouseOverWindow;
            if (window != null)
                window.wantsMouseMove = true;

            if (current.type == EventType.MouseMove)
            {
                window?.Repaint();
                HandleUtility.Repaint();
            }
        }

        private static void EnsureStyles()
        {
            _buttonStyle ??= new GUIStyle(EditorStyles.iconButton)
            {
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(2, 2, 2, 2),
                margin = new RectOffset(0, 0, 0, 0)
            };

            _copyIcon ??= GetIcon("TreeEditor.Duplicate", "Copy component");
            _pasteIcon ??= GetIcon("Clipboard", "Paste values into this component");
        }

        private static GUIContent GetIcon(string iconName, string tooltip)
        {
            GUIContent content = EditorGUIUtility.IconContent(iconName);
            if (content == null || content.image == null)
                content = new GUIContent(iconName == "Clipboard" ? "P" : "C");

            content.tooltip = tooltip;
            return content;
        }
    }
}