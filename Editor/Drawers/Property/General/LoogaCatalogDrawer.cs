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
        private const float DeleteButtonWidth = 46f;
        private const float AccentWidth = 4f;
        private const float TreeStep = 12f;

        private static readonly Color CatalogColor = new(0.155f, 0.155f, 0.155f, 1f);
        private static readonly Color RowColor = new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color RowHoverColor = new(0.20f, 0.20f, 0.20f, 1f);
        private static readonly Color EmptyColor = new(0.17f, 0.17f, 0.17f, 1f);
        private static readonly Color AccentColor = new(0.26f, 0.58f, 0.95f, 1f);
        private static readonly Color TreeLineColor = new(0.37f, 0.37f, 0.37f, 1f);

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

            Rect labelRect = new(rect.x + 8f, rect.y, rect.width - DeleteButtonWidth - 14f, rect.height);
            DrawEntryField(labelRect, definition, catalog, entryType);

            Rect deleteRect = new(rect.xMax - DeleteButtonWidth - 4f, rect.y + 3f, DeleteButtonWidth, rect.height - 6f);
            using (new EditorGUI.DisabledScope(!catalog.AllowDelete))
            {
                if (GUI.Button(deleteRect, "Del"))
                {
                    DeleteEntry(property, index, definition, catalog);
                }
            }

        }

        private static void DrawEntryField(Rect rect, ScriptableObject definition, LoogaCatalogAttribute catalog, Type entryType)
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

            EditorGUI.BeginChangeCheck();
            string editedLabel = EditorGUI.DelayedTextField(labelRect, label);
            if (EditorGUI.EndChangeCheck())
                RenameEntry(definition, catalog, editedLabel);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && iconRect.Contains(Event.current.mousePosition))
            {
                Selection.activeObject = definition;
                EditorGUIUtility.PingObject(definition);
                Event.current.Use();
            }
        }

        private static void RenameEntry(ScriptableObject definition, LoogaCatalogAttribute catalog, string rawName)
        {
            string newName = NormalizeEntryName(rawName, catalog);
            if (string.IsNullOrWhiteSpace(newName) || string.Equals(definition.name, newName, StringComparison.Ordinal))
                return;

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
            AssetDatabase.SaveAssets();
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
            int buttonCount = catalog.AllowAdd && canSync ? 2 : 1;
            float width = buttonCount == 2 ? (rect.width - Gap) * 0.5f : rect.width;
            Rect firstRect = new(rect.x, rect.y, width, rect.height);

            if (catalog.AllowAdd)
            {
                if (GUI.Button(firstRect, $"Add {ObjectNames.NicifyVariableName(entryType.Name)}"))
                {
                    AddEntry(property, catalog, entryType);
                }
            }
            else if (canSync && GUI.Button(firstRect, "Sync Sub-Assets"))
            {
                SyncFromSubAssets(property, entryType);
            }

            if (catalog.AllowAdd && canSync)
            {
                Rect syncRect = new(firstRect.xMax + Gap, rect.y, width, rect.height);
                if (GUI.Button(syncRect, "Sync Sub-Assets"))
                {
                    SyncFromSubAssets(property, entryType);
                }
            }
        }

        private static void AddEntry(SerializedProperty property, LoogaCatalogAttribute catalog, Type entryType)
        {
            UnityEngine.Object owner = property.serializedObject.targetObject;
            string assetPath = AssetDatabase.GetAssetPath(owner);
            if (catalog.StoreAsSubAssets && string.IsNullOrWhiteSpace(assetPath))
            {
                EditorUtility.DisplayDialog("Save Catalog First", "Save this catalog asset before adding nested definitions.", "OK");
                return;
            }

            ScriptableObject entry = ScriptableObject.CreateInstance(entryType);
            entry.name = GetUniqueName(owner, catalog, entryType);
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

        private static string GetUniqueName(UnityEngine.Object owner, LoogaCatalogAttribute catalog, Type entryType)
        {
            string baseName = !string.IsNullOrWhiteSpace(catalog.CreateName)
                ? catalog.CreateName
                : $"New {ObjectNames.NicifyVariableName(entryType.Name)}";

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
