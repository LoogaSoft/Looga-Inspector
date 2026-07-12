using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaComponentClipboard
    {
        private static readonly List<CopiedComponent> CopiedComponents = new();
        private static string _sourceName;

        public static bool HasClipboard => CopiedComponents.Count > 0;
        public static bool HasPasteableComponents
        {
            get
            {
                for (int i = 0; i < CopiedComponents.Count; i++)
                {
                    Type type = Type.GetType(CopiedComponents[i].TypeName);
                    if (IsPasteableComponentType(type))
                        return true;
                }

                return false;
            }
        }

        public static int CopiedCount => CopiedComponents.Count;

        public static void CopyComponents(GameObject source)
        {
            CopyComponents(source, null);
        }

        public static void CopyComponents(GameObject source, ICollection<int> selectedComponentIds)
        {
            CopiedComponents.Clear();
            _sourceName = source != null ? source.name : string.Empty;

            if (source == null)
                return;

            Component[] components = source.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (selectedComponentIds != null && selectedComponentIds.Count > 0 && (component == null || !selectedComponentIds.Contains(component.GetInstanceID())))
                    continue;

                CopyComponentIntoClipboard(component, source.name, false);
            }
        }

        public static void CopyComponent(Component component)
        {
            CopiedComponents.Clear();
            _sourceName = component != null ? component.gameObject.name : string.Empty;
            CopyComponentIntoClipboard(component, _sourceName, true);
        }

        public static void PasteComponents(Object[] targets)
        {
            if (!HasClipboard || targets == null)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paste Components");

            int pastedCount = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                GameObject targetGameObject = ResolveGameObject(targets[i]);
                if (targetGameObject == null)
                    continue;

                for (int componentIndex = 0; componentIndex < CopiedComponents.Count; componentIndex++)
                {
                    CopiedComponent copied = CopiedComponents[componentIndex];
                    Type type = Type.GetType(copied.TypeName);
                    if (!IsPasteableComponentType(type))
                        continue;

                    try
                    {
                        Component component = Undo.AddComponent(targetGameObject, type);
                        Undo.RecordObject(component, "Paste Component Values");
                        EditorJsonUtility.FromJsonOverwrite(copied.Json, component);
                        EditorUtility.SetDirty(component);
                        pastedCount++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[Looga Inspector] Could not paste component '{type.Name}' onto '{targetGameObject.name}'. {exception.Message}", targetGameObject);
                    }
                }

                EditorUtility.SetDirty(targetGameObject);
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (pastedCount > 0)
                Debug.Log($"[Looga Inspector] Pasted {pastedCount} copied component(s) from '{SourceLabel()}'.");
        }

        public static void PasteValuesIntoMatchingComponents(Object[] targets)
        {
            if (!HasClipboard || targets == null)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paste Component Values");

            int pastedCount = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                GameObject targetGameObject = ResolveGameObject(targets[i]);
                if (targetGameObject == null)
                    continue;

                Dictionary<Type, int> nextMatchByType = new();
                for (int componentIndex = 0; componentIndex < CopiedComponents.Count; componentIndex++)
                {
                    CopiedComponent copied = CopiedComponents[componentIndex];
                    Type type = Type.GetType(copied.TypeName);
                    if (!IsValuePasteableComponentType(type))
                        continue;

                    Component[] matchingComponents = targetGameObject.GetComponents(type);
                    nextMatchByType.TryGetValue(type, out int matchIndex);
                    nextMatchByType[type] = matchIndex + 1;

                    if (matchIndex >= matchingComponents.Length)
                        continue;

                    Component component = matchingComponents[matchIndex];
                    if (component == null)
                        continue;

                    try
                    {
                        Undo.RecordObject(component, "Paste Component Values");
                        EditorJsonUtility.FromJsonOverwrite(copied.Json, component);
                        EditorUtility.SetDirty(component);
                        pastedCount++;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogWarning($"[Looga Inspector] Could not paste values for component '{type.Name}' on '{targetGameObject.name}'. {exception.Message}", targetGameObject);
                    }
                }

                EditorUtility.SetDirty(targetGameObject);
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (pastedCount > 0)
                Debug.Log($"[Looga Inspector] Pasted values into {pastedCount} matching component(s) from '{SourceLabel()}'.");
        }

        public static bool CanPasteValuesIntoComponents(Object[] targets)
        {
            return TryGetCopiedComponentForTargets(targets, out _, out _);
        }

        public static void PasteValuesIntoComponents(Object[] targets)
        {
            if (!TryGetCopiedComponentForTargets(targets, out CopiedComponent copied, out Type type))
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Paste Component Values");

            int pastedCount = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not Component component || component.GetType() != type)
                    continue;

                try
                {
                    Undo.RecordObject(component, "Paste Component Values");
                    EditorJsonUtility.FromJsonOverwrite(copied.Json, component);
                    EditorUtility.SetDirty(component);
                    pastedCount++;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"[Looga Inspector] Could not paste values into component '{type.Name}' on '{component.gameObject.name}'. {exception.Message}", component);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (pastedCount > 0)
                Debug.Log($"[Looga Inspector] Pasted values into {pastedCount} selected component(s).");
        }

        private static void CopyComponentIntoClipboard(Component component, string sourceName, bool logFailures)
        {
            if (component == null)
                return;

            Type type = component.GetType();
            try
            {
                string typeName = type.AssemblyQualifiedName ?? string.Empty;
                if (string.IsNullOrWhiteSpace(typeName))
                    return;

                CopiedComponents.Add(new CopiedComponent(typeName, EditorJsonUtility.ToJson(component)));
            }
            catch (Exception exception)
            {
                if (logFailures)
                    Debug.LogWarning($"[Looga Inspector] Could not copy component '{type.Name}' from '{sourceName}'. {exception.Message}", component);
            }
        }

        private static bool TryGetCopiedComponentForTargets(Object[] targets, out CopiedComponent copied, out Type type)
        {
            copied = default;
            type = null;

            if (!HasClipboard || targets == null)
                return false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not Component component)
                    continue;

                Type componentType = component.GetType();
                for (int componentIndex = 0; componentIndex < CopiedComponents.Count; componentIndex++)
                {
                    CopiedComponent candidate = CopiedComponents[componentIndex];
                    Type candidateType = Type.GetType(candidate.TypeName);
                    if (candidateType != componentType)
                        continue;

                    copied = candidate;
                    type = candidateType;
                    return true;
                }
            }

            return false;
        }

        private static bool IsPasteableComponentType(Type type)
        {
            return type != null && typeof(Component).IsAssignableFrom(type) && !typeof(Transform).IsAssignableFrom(type);
        }

        private static bool IsValuePasteableComponentType(Type type)
        {
            return type != null && typeof(Component).IsAssignableFrom(type);
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

        private static string SourceLabel()
        {
            return string.IsNullOrWhiteSpace(_sourceName) ? "copied GameObject" : _sourceName;
        }

        private readonly struct CopiedComponent
        {
            public readonly string TypeName;
            public readonly string Json;

            public CopiedComponent(string typeName, string json)
            {
                TypeName = typeName;
                Json = json;
            }
        }
    }
}
