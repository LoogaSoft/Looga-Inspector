using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(LoogaCatalogAttribute))]
    public sealed class LoogaCatalogDrawer : PropertyDrawerBase
    {
        private const float Gap = 3f;
        private const float Padding = 4f;
        private const float RowHeight = 24f;
        private const float ButtonHeight = 24f;
        private const float IconButtonSize = 22f;
        private const float IconButtonGap = 3f;
        private const float AccentWidth = LoogaEditorStyle.AccentRailWidth;
        private const float TreeStep = 12f;
        private const float AddButtonWidth = 72f;
        private const float CancelButtonWidth = 72f;
        private const float SyncButtonWidth = 64f;
        private const string ActiveAddingKey = "LoogaCatalog_ActiveAddingKey";
        private const string ActivePendingNameKey = "LoogaCatalog_ActivePendingNameKey";
        private const string ActiveOwnerKey = "LoogaCatalog_ActiveOwner";

        private static Color CatalogColor => LoogaEditorStyle.BoxColor;
        private static Color RowColor => LoogaEditorStyle.ListRowColor;
        private static Color RowHoverColor => LoogaEditorStyle.ListHoverColor;
        private static Color EmptyColor => LoogaEditorStyle.ListRowColor;
        private static Color AccentColor => LoogaEditorStyle.ActionAccentColor;
        private static Color TreeLineColor => LoogaEditorStyle.TreeLineColor;

        static LoogaCatalogDrawer()
        {
            Selection.selectionChanged -= CancelPendingAddWhenSelectionLeavesCatalog;
            Selection.selectionChanged += CancelPendingAddWhenSelectionLeavesCatalog;
        }

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            LoogaCatalogAttribute catalog = (LoogaCatalogAttribute)attribute;
            Type entryType = GetEntryType(fieldInfo?.FieldType);
            if (!CanDraw(property, entryType))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Draw(position, property, catalog, entryType);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            Type entryType = GetEntryType(fieldInfo?.FieldType);
            if (!CanDraw(property, entryType))
                return EditorGUI.GetPropertyHeight(property, label, true);

            LoogaCatalogAttribute catalog = (LoogaCatalogAttribute)attribute;
            return GetHeight(property, catalog, entryType);
        }

        public static void Draw(Rect position, SerializedProperty property, LoogaCatalogAttribute catalog, Type entryType)
        {
            EditorGUI.DrawRect(position, CatalogColor);
            EditorGUI.DrawRect(new Rect(position.x, position.y, AccentWidth, position.height), AccentColor);

            Rect headerRect = new(position.x, position.y, position.width, RowHeight);
            DrawHeader(headerRect, property, catalog);

            Rect bodyRect = new(
                position.x,
                headerRect.yMax,
                position.width,
                position.height - RowHeight);

            DrawBody(bodyRect, property, catalog, entryType);
        }

        public static float GetHeight(SerializedProperty property, LoogaCatalogAttribute catalog, Type entryType)
        {
            float height = RowHeight + Padding * 2f;

            if (catalog.AllowAdd || CanSyncFromSubAssets(property, entryType))
                height += ButtonHeight + Gap;

            height += property.arraySize > 0 ? property.arraySize * RowHeight : RowHeight;

            return height;
        }

        public static bool CanDraw(SerializedProperty property, Type entryType)
        {
            return property.isArray
                && property.propertyType != SerializedPropertyType.String
                && entryType != null
                && typeof(ScriptableObject).IsAssignableFrom(entryType);
        }

        public static Type GetEntryType(Type fieldType)
        {
            if (fieldType == null)
                return null;

            if (fieldType.IsArray)
                return fieldType.GetElementType();

            if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                return fieldType.GetGenericArguments()[0];

            return null;
        }

        private static void DrawHeader(Rect rect, SerializedProperty property, LoogaCatalogAttribute catalog)
        {
            Rect labelRect = new(rect.x + AccentWidth + Padding, rect.y, rect.width - AccentWidth - Padding * 2f, rect.height);
            string title = string.IsNullOrWhiteSpace(catalog.Title) ? property.displayName : catalog.Title;
            EditorGUI.LabelField(labelRect, $"{title} ({property.arraySize})", EditorStyles.boldLabel);
        }

        private static void DrawBody(Rect rect, SerializedProperty property, LoogaCatalogAttribute catalog, Type entryType)
        {
            Rect contentRect = new(rect.x + AccentWidth + Padding, rect.y + Padding, rect.width - AccentWidth - Padding * 2f, rect.height - Padding * 2f);
            float y = contentRect.y;
            bool canSync = CanSyncFromSubAssets(property, entryType);

            if (catalog.AllowAdd || canSync)
            {
                DrawButtons(new Rect(contentRect.x, y, contentRect.width, ButtonHeight), property, catalog, entryType, canSync);
                y += ButtonHeight + Gap;
            }

            if (property.arraySize == 0)
            {
                Rect emptyRect = new(contentRect.x, y, contentRect.width, RowHeight);
                EditorGUI.DrawRect(emptyRect, EmptyColor);
                EditorGUI.LabelField(new Rect(emptyRect.x + 8f, emptyRect.y, emptyRect.width - 16f, emptyRect.height), $"No {ObjectNames.NicifyVariableName(entryType.Name)} entries.");
                y = emptyRect.yMax + Gap;
            }
            else
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    Rect rowRect = new(contentRect.x, y, contentRect.width, RowHeight);
                    DrawRow(rowRect, property, i, catalog, entryType);
                    y += RowHeight;
                }

                y += Gap;
            }
        }

        private static void DrawRow(Rect rect, SerializedProperty property, int index, LoogaCatalogAttribute catalog, Type entryType)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            ScriptableObject definition = element.objectReferenceValue as ScriptableObject;
            bool hovering = rect.Contains(Event.current.mousePosition);
            EditorGUI.DrawRect(rect, hovering ? RowHoverColor : RowColor);

            float actionsWidth = IconButtonSize * 2f + IconButtonGap + 8f;
            Rect labelRect = new(rect.x + 8f, rect.y, rect.width - actionsWidth - 8f, rect.height);
            DrawEntryContent(labelRect, property, definition, catalog, entryType);

            Rect editRect = new(rect.xMax - IconButtonSize * 2f - IconButtonGap - 4f, rect.y + 1f, IconButtonSize, IconButtonSize);
            using (new EditorGUI.DisabledScope(definition == null))
            {
                if (GUI.Button(editRect, GetEditContent(), EditorStyles.miniButtonLeft))
                    ToggleEditing(definition);
            }

            Rect deleteRect = new(editRect.xMax + IconButtonGap, rect.y + 1f, IconButtonSize, IconButtonSize);
            using (new EditorGUI.DisabledScope(!catalog.AllowDelete))
            {
                if (GUI.Button(deleteRect, GetDeleteContent(), EditorStyles.miniButtonRight))
                {
                    DeleteEntry(property, index, definition, catalog);
                }
            }

        }

        private static void DrawEntryContent(Rect rect, SerializedProperty property, ScriptableObject definition, LoogaCatalogAttribute catalog, Type entryType)
        {
            GUIContent content = definition != null
                ? EditorGUIUtility.ObjectContent(definition, entryType)
                : new GUIContent("<Missing>");

            string label = definition != null ? GetEntryLabel(definition, catalog) : "<Missing>";
            int depth = GetTreeDepth(label, catalog);
            float treeIndent = depth * TreeStep;
            Rect iconRect = new(rect.x + treeIndent, rect.y + 4f, 16f, 16f);
            Rect labelRect = new(rect.x + treeIndent + 20f, rect.y, rect.width - treeIndent - 20f, rect.height);

            DrawTreeLines(rect, depth);

            if (content.image != null)
                GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit);

            if (definition == null)
            {
                EditorGUI.LabelField(labelRect, label);
                return;
            }

            if (IsEditing(definition))
            {
                EditorGUI.BeginChangeCheck();
                string editedLabel = EditorGUI.DelayedTextField(labelRect, label);
                if (EditorGUI.EndChangeCheck())
                {
                    RenameEntry(property, definition, catalog, editedLabel);
                    SetEditing(definition, false);
                }
            }
            else
            {
                EditorGUI.LabelField(labelRect, label);

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                    Event.current.Use();
                }
            }
        }

        private static GUIContent GetEditContent()
        {
            GUIContent content = EditorGUIUtility.IconContent("editicon.sml");
            content.tooltip = "Edit";
            if (content.image == null && string.IsNullOrEmpty(content.text))
                content.text = "E";

            return content;
        }

        private static GUIContent GetDeleteContent()
        {
            GUIContent content = EditorGUIUtility.IconContent("TreeEditor.Trash");
            content.tooltip = "Delete";
            if (content.image == null && string.IsNullOrEmpty(content.text))
                content.text = "X";

            return content;
        }

        private static bool IsEditing(ScriptableObject definition)
        {
            return definition != null && SessionState.GetBool(GetEditingKey(definition), false);
        }

        private static void ToggleEditing(ScriptableObject definition)
        {
            if (definition == null)
                return;

            SetEditing(definition, !IsEditing(definition));
        }

        private static void SetEditing(ScriptableObject definition, bool editing)
        {
            if (definition != null)
                SessionState.SetBool(GetEditingKey(definition), editing);
        }

        private static string GetEditingKey(ScriptableObject definition)
        {
            return $"LoogaCatalog_Edit_{definition.GetInstanceID()}";
        }

        private static void RenameEntry(SerializedProperty property, ScriptableObject definition, LoogaCatalogAttribute catalog, string rawName)
        {
            string newName = NormalizeEntryName(rawName, catalog);
            string oldName = GetEntryLabel(definition, catalog);
            if (string.IsNullOrWhiteSpace(newName) || string.Equals(oldName, newName, StringComparison.Ordinal))
                return;

            RenameSingleEntry(definition, catalog, newName);

            if (!string.IsNullOrWhiteSpace(catalog.TreePath))
                RenameChildEntries(property, definition, catalog, oldName, newName);

            AssetDatabase.SaveAssets();
        }

        private static void RenameSingleEntry(ScriptableObject definition, LoogaCatalogAttribute catalog, string newName)
        {
            Undo.RecordObject(definition, "Rename Catalog Entry");
            definition.name = newName;

            if (!string.IsNullOrWhiteSpace(catalog.TreePath))
            {
                SerializedObject serializedDefinition = new(definition);
                SerializedProperty treePath = serializedDefinition.FindProperty(catalog.TreePath);
                if (treePath != null && treePath.propertyType == SerializedPropertyType.String)
                {
                    treePath.stringValue = newName;
                    serializedDefinition.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(definition);
        }

        private static void RenameChildEntries(
            SerializedProperty property,
            ScriptableObject renamedDefinition,
            LoogaCatalogAttribute catalog,
            string oldParentPath,
            string newParentPath)
        {
            if (property == null || string.IsNullOrWhiteSpace(oldParentPath))
                return;

            string oldPrefix = $"{oldParentPath}.";
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue is not ScriptableObject child || child == renamedDefinition)
                    continue;

                string childPath = GetEntryLabel(child, catalog);
                if (!childPath.StartsWith(oldPrefix, StringComparison.Ordinal))
                    continue;

                string childSuffix = childPath.Substring(oldPrefix.Length);
                RenameSingleEntry(child, catalog, $"{newParentPath}.{childSuffix}");
            }
        }

        private static string NormalizeEntryName(string rawName, LoogaCatalogAttribute catalog)
        {
            string name = string.IsNullOrWhiteSpace(rawName) ? string.Empty : rawName.Trim();
            if (string.IsNullOrWhiteSpace(catalog.TreePath))
                return name;

            return name.Replace('/', '.').Replace('\\', '.').Replace(' ', '.');
        }

        private static int GetTreeDepth(string label, LoogaCatalogAttribute catalog)
        {
            if (string.IsNullOrWhiteSpace(catalog.TreePath))
                return 0;

            int depth = 0;
            for (int i = 0; i < label.Length; i++)
            {
                if (label[i] == '.')
                    depth++;
            }

            return depth;
        }

        private static void DrawTreeLines(Rect rect, int depth)
        {
            if (depth <= 0)
                return;

            float centerY = Mathf.Round(rect.y + rect.height * 0.5f);
            float startX = rect.x + 8f;
            for (int i = 0; i < depth; i++)
            {
                float x = Mathf.Round(startX + i * TreeStep);
                EditorGUI.DrawRect(new Rect(x, rect.y, 1f, rect.height), TreeLineColor);
            }

            float lastX = Mathf.Round(startX + (depth - 1) * TreeStep);
            float endX = Mathf.Round(rect.x + depth * TreeStep - 2f);
            EditorGUI.DrawRect(new Rect(lastX, centerY, Mathf.Max(1f, endX - lastX), 1f), TreeLineColor);
        }

        private static string GetEntryLabel(ScriptableObject definition, LoogaCatalogAttribute catalog)
        {
            if (definition == null)
                return "<Missing>";

            if (!string.IsNullOrWhiteSpace(catalog.TreePath))
            {
                SerializedObject serializedDefinition = new(definition);
                SerializedProperty treePath = serializedDefinition.FindProperty(catalog.TreePath);
                if (treePath != null && treePath.propertyType == SerializedPropertyType.String && !string.IsNullOrWhiteSpace(treePath.stringValue))
                    return treePath.stringValue;
            }

            return definition.name;
        }

        private static void DrawButtons(Rect rect, SerializedProperty property, LoogaCatalogAttribute catalog, Type entryType, bool canSync)
        {
            if (catalog.AllowAdd)
            {
                string nameKey = GetPendingNameKey(property, entryType);
                string addingKey = GetAddingKey(property, entryType);
                float syncWidth = canSync ? SyncButtonWidth + Gap : 0f;
                bool isAdding = SessionState.GetBool(addingKey, false);

                if (!isAdding)
                {
                    Rect addRect = new(rect.x, rect.y, Mathf.Max(80f, rect.width - syncWidth), rect.height);
                    if (GUI.Button(addRect, $"Add {ObjectNames.NicifyVariableName(entryType.Name)}"))
                    {
                        SessionState.SetBool(addingKey, true);
                        SessionState.SetString(nameKey, GetDefaultCreateName(catalog, entryType));
                        SetActiveAddState(property, addingKey, nameKey);
                    }

                    if (canSync)
                    {
                        Rect syncRect = new(addRect.xMax + Gap, rect.y, SyncButtonWidth, rect.height);
                        if (GUI.Button(syncRect, "Sync"))
                        {
                            SyncFromSubAssets(property, entryType);
                        }
                    }

                    return;
                }

                Rect nameRect = new(rect.x, rect.y, Mathf.Max(80f, rect.width - AddButtonWidth - CancelButtonWidth - Gap * 2f - syncWidth), rect.height);
                Rect createRect = new(nameRect.xMax + Gap, rect.y, AddButtonWidth, rect.height);
                Rect cancelRect = new(createRect.xMax + Gap, rect.y, CancelButtonWidth, rect.height);

                EditorGUI.BeginChangeCheck();
                string pendingName = EditorGUI.TextField(nameRect, SessionState.GetString(nameKey, string.Empty));
                if (EditorGUI.EndChangeCheck())
                {
                    SessionState.SetString(nameKey, pendingName);
                }

                if (GUI.Button(createRect, "Create"))
                {
                    AddEntry(property, catalog, entryType, pendingName);
                    CancelPendingAdd(addingKey, nameKey);
                }

                if (GUI.Button(cancelRect, "Cancel"))
                {
                    CancelPendingAdd(addingKey, nameKey);
                }

                if (canSync)
                {
                    Rect syncRect = new(cancelRect.xMax + Gap, rect.y, SyncButtonWidth, rect.height);
                    if (GUI.Button(syncRect, "Sync"))
                    {
                        SyncFromSubAssets(property, entryType);
                    }
                }

                return;
            }

            if (canSync && GUI.Button(rect, "Sync"))
                SyncFromSubAssets(property, entryType);
        }

        private static void SetActiveAddState(SerializedProperty property, string addingKey, string nameKey)
        {
            UnityEngine.Object owner = property.serializedObject.targetObject;
            SessionState.SetString(ActiveAddingKey, addingKey);
            SessionState.SetString(ActivePendingNameKey, nameKey);
            SessionState.SetInt(ActiveOwnerKey, owner != null ? owner.GetInstanceID() : 0);
        }

        private static void CancelPendingAdd(string addingKey, string nameKey)
        {
            SessionState.SetBool(addingKey, false);
            SessionState.EraseString(nameKey);
            SessionState.EraseString(ActiveAddingKey);
            SessionState.EraseString(ActivePendingNameKey);
            SessionState.EraseInt(ActiveOwnerKey);
            GUI.FocusControl(null);
        }

        private static void CancelPendingAddWhenSelectionLeavesCatalog()
        {
            string addingKey = SessionState.GetString(ActiveAddingKey, string.Empty);
            if (string.IsNullOrEmpty(addingKey))
                return;

            int ownerId = SessionState.GetInt(ActiveOwnerKey, 0);
            UnityEngine.Object selected = Selection.activeObject;
            if (selected != null && selected.GetInstanceID() == ownerId)
                return;

            string nameKey = SessionState.GetString(ActivePendingNameKey, string.Empty);
            SessionState.SetBool(addingKey, false);
            if (!string.IsNullOrEmpty(nameKey))
                SessionState.EraseString(nameKey);

            SessionState.EraseString(ActiveAddingKey);
            SessionState.EraseString(ActivePendingNameKey);
            SessionState.EraseInt(ActiveOwnerKey);
        }

        private static void AddEntry(SerializedProperty property, LoogaCatalogAttribute catalog, Type entryType, string requestedName)
        {
            UnityEngine.Object owner = property.serializedObject.targetObject;
            string assetPath = AssetDatabase.GetAssetPath(owner);
            if (catalog.StoreAsSubAssets && string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog("Save Catalog First", "Save this catalog asset before adding nested definitions.", "OK");
                return;
            }

            ScriptableObject entry = ScriptableObject.CreateInstance(entryType);
            entry.name = GetUniqueName(owner, catalog, entryType, requestedName);
            InitializeTreePath(entry, catalog);

            Undo.RegisterCreatedObjectUndo(entry, $"Add {ObjectNames.NicifyVariableName(entryType.Name)}");
            if (catalog.StoreAsSubAssets)
            {
                AssetDatabase.AddObjectToAsset(entry, owner);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath);
            }

            property.serializedObject.Update();
            int index = property.arraySize;
            property.arraySize++;
            property.GetArrayElementAtIndex(index).objectReferenceValue = entry;
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(owner);

            Selection.activeObject = entry;
            EditorGUIUtility.PingObject(entry);
        }

        private static string GetPendingNameKey(SerializedProperty property, Type entryType)
        {
            UnityEngine.Object owner = property.serializedObject.targetObject;
            int ownerId = owner != null ? owner.GetInstanceID() : 0;
            return $"LoogaCatalog_PendingName_{ownerId}_{property.propertyPath}_{entryType.FullName}";
        }

        private static string GetAddingKey(SerializedProperty property, Type entryType)
        {
            UnityEngine.Object owner = property.serializedObject.targetObject;
            int ownerId = owner != null ? owner.GetInstanceID() : 0;
            return $"LoogaCatalog_Adding_{ownerId}_{property.propertyPath}_{entryType.FullName}";
        }

        private static void DeleteEntry(SerializedProperty property, int index, ScriptableObject definition, LoogaCatalogAttribute catalog)
        {
            if (definition == null)
            {
                RemoveReferenceAt(property, index);
                return;
            }

            bool isSubAsset = AssetDatabase.IsSubAsset(definition);
            string action = isSubAsset && catalog.StoreAsSubAssets ? "Delete" : "Remove";
            string message = isSubAsset && catalog.StoreAsSubAssets
                ? $"Delete nested definition '{definition.name}' from this catalog?"
                : $"Remove reference '{definition.name}' from this catalog? The asset itself will not be deleted.";

            if (!EditorUtility.DisplayDialog($"{action} Catalog Entry", message, action, "Cancel"))
                return;

            if (isSubAsset && catalog.StoreAsSubAssets)
            {
                UnityEngine.Object owner = property.serializedObject.targetObject;
                string assetPath = AssetDatabase.GetAssetPath(owner);
                Undo.RecordObject(owner, "Delete Catalog Entry");
                RemoveReferenceAt(property, index);
                UnityEngine.Object.DestroyImmediate(definition, allowDestroyingAssets: true);
                AssetDatabase.SaveAssets();
                AssetDatabase.ImportAsset(assetPath);
                return;
            }

            RemoveReferenceAt(property, index);
        }

        private static void RemoveReferenceAt(SerializedProperty property, int index)
        {
            property.serializedObject.Update();
            SerializedProperty element = property.GetArrayElementAtIndex(index);
            element.objectReferenceValue = null;
            property.DeleteArrayElementAtIndex(index);
            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        private static bool CanSyncFromSubAssets(SerializedProperty property, Type entryType)
        {
            string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
            if (string.IsNullOrWhiteSpace(assetPath))
                return false;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != property.serializedObject.targetObject && entryType.IsInstanceOfType(assets[i]))
                    return true;
            }

            return false;
        }

        private static void SyncFromSubAssets(SerializedProperty property, Type entryType)
        {
            string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            List<ScriptableObject> entries = new(assets.Length);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != property.serializedObject.targetObject && assets[i] is ScriptableObject scriptable && entryType.IsInstanceOfType(scriptable))
                {
                    entries.Add(scriptable);
                }
            }

            entries.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));

            property.serializedObject.Update();
            property.arraySize = entries.Count;
            for (int i = 0; i < entries.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }

            property.serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        private static string GetUniqueName(UnityEngine.Object owner, LoogaCatalogAttribute catalog, Type entryType, string requestedName)
        {
            string normalizedRequest = NormalizeEntryName(requestedName, catalog);
            string baseName = !string.IsNullOrWhiteSpace(normalizedRequest)
                ? normalizedRequest
                : GetDefaultCreateName(catalog, entryType);

            if (owner == null)
                return baseName;

            string assetPath = AssetDatabase.GetAssetPath(owner);
            UnityEngine.Object[] assets = string.IsNullOrWhiteSpace(assetPath)
                ? Array.Empty<UnityEngine.Object>()
                : AssetDatabase.LoadAllAssetsAtPath(assetPath);

            string candidate = baseName;
            int suffix = 1;
            while (NameExists(assets, candidate))
            {
                suffix++;
                candidate = $"{baseName} {suffix}";
            }

            return candidate;
        }

        private static string GetDefaultCreateName(LoogaCatalogAttribute catalog, Type entryType)
        {
            return !string.IsNullOrWhiteSpace(catalog.CreateName)
                ? catalog.CreateName
                : $"New {ObjectNames.NicifyVariableName(entryType.Name)}";
        }

        private static bool NameExists(UnityEngine.Object[] assets, string name)
        {
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null && string.Equals(assets[i].name, name, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void InitializeTreePath(ScriptableObject entry, LoogaCatalogAttribute catalog)
        {
            if (entry == null || string.IsNullOrWhiteSpace(catalog.TreePath))
                return;

            SerializedObject serializedEntry = new(entry);
            SerializedProperty treePath = serializedEntry.FindProperty(catalog.TreePath);
            if (treePath == null || treePath.propertyType != SerializedPropertyType.String || !string.IsNullOrWhiteSpace(treePath.stringValue))
                return;

            treePath.stringValue = entry.name.Replace(' ', '.');
            serializedEntry.ApplyModifiedPropertiesWithoutUndo();
        }

    }
}
