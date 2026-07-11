using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    [CustomEditor(typeof(Object), true)]
    [CanEditMultipleObjects]
    public class LoogaEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, HashSet<int>> _listSelectedIndices = new();
        private readonly Dictionary<string, int> _listSelectionAnchors = new();
        private string _draggingListKey = string.Empty;
        private int _draggingListIndex = -1;
        private int _draggingListDropIndex = -1;
        private float _draggingListMouseOffsetY;
        private static readonly Dictionary<Type, InspectorLayout> _layoutCache = new();
        private static readonly Dictionary<Type, LoogaInspectorMessageAttribute[]> _messageCache = new();
        private static readonly Dictionary<Type, StatusBoxAttribute[]> _statusBoxCache = new();
        private static readonly Dictionary<Type, OpenEditorWindowAttribute[]> _openWindowCache = new();
        
        #region Built-In
        private void OnDisable()
        {
            _listSelectedIndices.Clear();
            _listSelectionAnchors.Clear();
            _draggingListKey = string.Empty;
            _draggingListIndex = -1;
            _draggingListDropIndex = -1;
            _draggingListMouseOffsetY = 0f;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            var rootProperties = GetSerializedProperties();
            
            InspectorLayout layout = GetLayoutForType(target.GetType());
            
            SerializedProperty scriptProperty = serializedObject.FindProperty("m_Script");
            if (scriptProperty != null)
            {
                using (new EditorGUI.DisabledScope(true))
                    EditorGUILayout.PropertyField(scriptProperty);
            }
            
            EditorGUILayout.Space(1f);

            DrawHeaderAttributes(target.GetType());
            
            DrawButtons(layout, true);

            DrawPropertiesScope(rootProperties, target.GetType(), "");
            DrawUnmatchedSerializedProperties(rootProperties, layout);

            DrawButtons(layout, false);
            
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
        
        #region Drawers
        protected void DrawHeaderAttributes(Type inspectedType)
        {
            DrawInspectorMessages(inspectedType);
            DrawStatusBoxes(inspectedType);
            DrawOpenEditorWindowButtons(inspectedType);
        }

        private void DrawInspectorMessages(Type inspectedType)
        {
            LoogaInspectorMessageAttribute[] messages = GetInspectorMessages(inspectedType);
            if (messages.Length == 0)
                return;

            for (int i = 0; i < messages.Length; i++)
            {
                LoogaInspectorMessageAttribute message = messages[i];
                if (!ShouldDrawInspectorMessage(message))
                    continue;

                EditorGUILayout.HelpBox(message.Message, ValidateInputDrawer.GetMessageType(message.MessageMode));
                EditorGUILayout.Space(1f);
            }
        }

        private bool ShouldDrawInspectorMessage(LoogaInspectorMessageAttribute message)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                bool condition = string.IsNullOrWhiteSpace(message.Condition)
                    || ValidateInputDrawer.GetCondition(targets[i], message.Condition);

                if (message.Invert)
                    condition = !condition;

                if (condition)
                    return true;
            }

            return false;
        }

        private void DrawStatusBoxes(Type inspectedType)
        {
            StatusBoxAttribute[] statusBoxes = GetStatusBoxes(inspectedType);
            if (statusBoxes.Length == 0)
                return;

            for (int i = 0; i < statusBoxes.Length; i++)
            {
                StatusBoxAttribute statusBox = statusBoxes[i];
                if (!ShouldDrawStatusBox(statusBox, out string message))
                    continue;

                EditorGUILayout.HelpBox(message, StatusBoxDrawer.ToMessageType(statusBox.Type));
                DrawStatusBoxAction(statusBox);
                EditorGUILayout.Space(1f);
            }
        }

        private static void DrawStatusBoxAction(StatusBoxAttribute statusBox)
        {
            if (statusBox == null || string.IsNullOrWhiteSpace(statusBox.ButtonLabel))
                return;

            bool hasAssetPath = !string.IsNullOrWhiteSpace(statusBox.AssetPath);
            bool hasMenuPath = !string.IsNullOrWhiteSpace(statusBox.MenuPath);
            if (!hasAssetPath && !hasMenuPath)
                return;

            if (!GUILayout.Button(statusBox.ButtonLabel))
                return;

            if (hasAssetPath)
            {
                SelectAssetAtPath(statusBox.AssetPath);
                return;
            }

            EditorApplication.ExecuteMenuItem(statusBox.MenuPath);
        }

        private static void SelectAssetAtPath(string assetPath)
        {
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog(
                    "Asset Not Found",
                    $"No asset was found at:\n{assetPath}",
                    "OK");
                return;
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }

        private bool ShouldDrawStatusBox(StatusBoxAttribute statusBox, out string message)
        {
            message = string.Empty;
            if (statusBox == null)
                return false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (!StatusBoxDrawer.ShouldShow(targets[i], statusBox))
                    continue;

                message = StatusBoxDrawer.ResolveMessage(targets[i], statusBox);
                if (!string.IsNullOrWhiteSpace(message))
                    return true;
            }

            return false;
        }

        private void DrawOpenEditorWindowButtons(Type inspectedType)
        {
            OpenEditorWindowAttribute[] openWindows = GetOpenEditorWindowAttributes(inspectedType);
            if (openWindows.Length == 0)
                return;

            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < openWindows.Length; i++)
            {
                OpenEditorWindowAttribute openWindow = openWindows[i];
                using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(openWindow.MenuPath)))
                {
                    if (GUILayout.Button(openWindow.Label))
                        EditorApplication.ExecuteMenuItem(openWindow.MenuPath);
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(1f);
        }

        private void DrawPropertiesScope(List<SerializedProperty> properties, Type scopeType, string basePath)
        {
            if (properties.Count == 0)
                return;
            
            InspectorLayout layout = GetLayoutForType(scopeType);
            
            if (layout.HasTabs)
                DrawPropertiesWithTabs(properties, layout, scopeType, basePath);
            else
                DrawPropertySequence(layout.elements, properties, scopeType, basePath);
        }

        private void DrawPropertiesWithTabs(List<SerializedProperty> properties, InspectorLayout layout, Type scopeType, string basePath)
        {
            int tabGroupIndex = 0;
            int index = 0;

            while (index < layout.elements.Count)
            {
                InspectorElement element = layout.elements[index];
                if (!element.inTabGroup)
                {
                    List<InspectorElement> chunk = new();
                    while (index < layout.elements.Count && !layout.elements[index].inTabGroup)
                    {
                        chunk.Add(layout.elements[index]);
                        index++;
                    }

                    DrawPropertySequence(chunk, properties, scopeType, basePath);
                    continue;
                }

                List<InspectorElement> tabChunk = new();
                while (index < layout.elements.Count && layout.elements[index].inTabGroup)
                {
                    tabChunk.Add(layout.elements[index]);
                    index++;
                }

                if (tabGroupIndex >= layout.tabGroups.Count)
                {
                    tabGroupIndex++;
                    continue;
                }

                if (index > tabChunk.Count)
                    EditorGUILayout.Space();

                EditorGUILayout.BeginVertical(LoogaEditorFoldouts.SmallBoxStyle);

                DrawTabLevel(tabChunk, properties, scopeType, $"{basePath}_TabGroup{tabGroupIndex}", 0);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
                tabGroupIndex++;
            }
        }

        private void DrawTabLevel(
            List<InspectorElement> elements,
            List<SerializedProperty> properties,
            Type scopeType,
            string basePath,
            int level)
        {
            List<string> tabNames = GetTabNamesAtLevel(elements, level);
            if (tabNames.Count == 0)
            {
                DrawPropertySequence(elements, properties, scopeType, basePath);
                return;
            }

            string stateKey = GetTabStateKey(scopeType, basePath, level);
            int currentTabIndex = SessionState.GetInt(stateKey, 0);
            currentTabIndex = Mathf.Clamp(currentTabIndex, 0, tabNames.Count - 1);

            int newIndex = LoogaEditorTabs.DrawWrappingToolbar(
                currentTabIndex,
                tabNames.ToArray(),
                $"{basePath}_Level{level}_toolbar");

            if (newIndex != currentTabIndex)
            {
                SessionState.SetInt(stateKey, newIndex);
                currentTabIndex = newIndex;
            }

            string currentTabName = tabNames[currentTabIndex];
            List<InspectorElement> activeElements = new();
            for (int i = 0; i < elements.Count; i++)
            {
                InspectorElement element = elements[i];
                if (element.tabPath.Count > level && element.tabPath[level] == currentTabName)
                    activeElements.Add(element);
            }

            DrawSelectedTabContent(activeElements, properties, scopeType, $"{basePath}_{currentTabName}", level);
        }

        private void DrawSelectedTabContent(
            List<InspectorElement> activeElements,
            List<SerializedProperty> properties,
            Type scopeType,
            string basePath,
            int level)
        {
            int index = 0;
            while (index < activeElements.Count)
            {
                bool nested = activeElements[index].tabPath.Count > level + 1;
                List<InspectorElement> chunk = new();

                while (index < activeElements.Count
                       && (activeElements[index].tabPath.Count > level + 1) == nested)
                {
                    chunk.Add(activeElements[index]);
                    index++;
                }

                if (nested)
                    DrawTabLevel(chunk, properties, scopeType, $"{basePath}_Nested{level + 1}", level + 1);
                else
                    DrawPropertySequence(chunk, properties, scopeType, basePath);
            }
        }

        private static List<string> GetTabNamesAtLevel(List<InspectorElement> elements, int level)
        {
            List<string> tabNames = new();

            foreach (InspectorElement element in elements)
            {
                if (element.tabPath.Count <= level)
                    continue;

                string tabName = element.tabPath[level];
                if (!tabNames.Contains(tabName))
                    tabNames.Add(tabName);
            }

            return tabNames;
        }

        private static void ApplyTabAttribute(List<string> currentTabPath, TabAttribute tabAttribute)
        {
            int targetLevel = Mathf.Clamp(tabAttribute.level, 0, currentTabPath.Count);

            if (currentTabPath.Count > targetLevel)
                currentTabPath.RemoveRange(targetLevel, currentTabPath.Count - targetLevel);

            currentTabPath.Add(tabAttribute.tabName);
        }

        private void DrawPropertySequence(
            List<InspectorElement> elements,
            List<SerializedProperty> properties,
            Type scopeType,
            string basePath)
        {
            for (int i = 0; i < elements.Count; i++)
            {
                InspectorElement element = elements[i];
                if (!element.inStyledGroup)
                {
                    if (TryDrawInlineRow(elements, properties, ref i))
                        continue;

                    SerializedProperty property = FindSerializedPropertyByName(properties, element.propertyName);
                    if (property != null)
                        DrawCustomPropertyField(property, element.metadata);

                    continue;
                }

                List<SerializedProperty> groupProperties = new();
                InspectorElement groupStart = element;
                string groupName = groupStart.styledGroupName;
                bool isFoldout = groupStart.styledGroupIsFoldout;
                bool isToggleFoldout = groupStart.styledGroupIsToggleFoldout;

                while (i < elements.Count)
                {
                    InspectorElement groupElement = elements[i];
                    if (!groupElement.inStyledGroup
                        || groupElement.styledGroupName != groupName
                        || groupElement.styledGroupIsFoldout != isFoldout
                        || groupElement.styledGroupIsToggleFoldout != isToggleFoldout)
                    {
                        i--;
                        break;
                    }

                    SerializedProperty groupProperty = FindSerializedPropertyByName(properties, groupElement.propertyName);
                    if (groupProperty != null)
                        groupProperties.Add(groupProperty);

                    if (groupElement.endsStyledGroup)
                        break;

                    i++;
                }

                if (groupProperties.Count > 0)
                    DrawStyledGroup(groupStart, groupProperties, scopeType, basePath);
            }
        }

        private void DrawUnmatchedSerializedProperties(List<SerializedProperty> properties, InspectorLayout layout)
        {
            bool drewProperty = false;

            for (int i = 0; i < properties.Count; i++)
            {
                SerializedProperty property = properties[i];
                if (layout.propertyNames.Contains(property.name))
                    continue;

                DrawCustomPropertyField(property);
                drewProperty = true;
            }

            if (drewProperty)
                EditorGUILayout.Space(1f);
        }
        private void DrawCustomPropertyField(SerializedProperty property, InspectorPropertyMetadata metadata = null)
        {
            if (!PropertyUtils.IsVisible(property))
                return;
            
            DecoratorSystem.DrawDecorators(property, target);
            
            bool propertyEnabled = PropertyUtils.IsEnabled(property);
            bool isList = property.isArray && property.propertyType != SerializedPropertyType.String;
            LoogaCatalogAttribute catalogAttribute = PropertyUtils.GetAttribute<LoogaCatalogAttribute>(property);
              
            //disable GUI (making the field readonly) if enabled is false
            using (new EditorGUI.DisabledScope(disabled: !propertyEnabled))
            {
                if (catalogAttribute != null && TryDrawCatalogProperty(property, metadata, catalogAttribute))
                    return;
                else if (isList)
                    DrawLoogaList(property);
                else
                {
                    EditorGUI.BeginChangeCheck();

                    InlineRowAttribute inlineTypeAttribute = GetStructuredInlineRowAttribute(property, metadata);
                    StructBoxAttribute structBoxAttribute = GetStructuredBoxAttribute(property);
                    bool drewStructuredProperty = inlineTypeAttribute != null
                        && TryDrawInlineTypeProperty(property, GetPropertyLabel(property, metadata));

                    if (!drewStructuredProperty && structBoxAttribute != null)
                    {
                        DrawStructBoxProperty(property, structBoxAttribute, metadata);
                        drewStructuredProperty = true;
                    }

                    if (!drewStructuredProperty)
                    {
                        LoogaBoxAttribute boxAttribute = metadata?.boxAttribute ?? PropertyUtils.GetAttribute<LoogaBoxAttribute>(property);
                        LoogaFoldoutAttribute foldoutAttribute = metadata?.foldoutAttribute ?? PropertyUtils.GetAttribute<LoogaFoldoutAttribute>(property);
                        LoogaToggleFoldoutAttribute toggleFoldoutAttribute = metadata?.toggleFoldoutAttribute ?? PropertyUtils.GetAttribute<LoogaToggleFoldoutAttribute>(property);
                        if (toggleFoldoutAttribute != null)
                        {
                            DrawToggleFoldoutProperty(property, toggleFoldoutAttribute, metadata);
                        }
                        else if (foldoutAttribute != null)
                        {
                            DrawFoldoutProperty(property, foldoutAttribute, metadata);
                        }
                        else if (boxAttribute != null)
                        {
                            DrawBoxProperty(property, boxAttribute, metadata);
                        }
                        else
                        {
                            bool hasCustomDrawer = metadata?.hasCustomDrawer ?? CustomDrawerUtil.HasCustomDrawer(property);

                            bool customNestedFoldout = ShouldDrawNestedFoldout(property, hasCustomDrawer);
                            if (customNestedFoldout)
                            {
                                DrawNestedFoldoutProperty(property, metadata);
                            }
                            else
                            {
                                EditorGUILayout.PropertyField(property, GetPropertyLabel(property, metadata), false);

                                if (!hasCustomDrawer && property.propertyType == SerializedPropertyType.Generic &&
                                    property.hasVisibleChildren && property.isExpanded)
                                    DrawNestedPropertyChildren(property);
                            }
                        }
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                        PropertyUtils.CallOnFieldChangedCallbacks(property);
                }
              }
          }

        private bool TryDrawCatalogProperty(
            SerializedProperty property,
            InspectorPropertyMetadata metadata,
            LoogaCatalogAttribute catalogAttribute)
        {
            FieldInfo fieldInfo = metadata?.fieldInfo ?? ReflectionUtils.GetField(target.GetType(), property.name);
            Type entryType = LoogaCatalogDrawer.GetEntryType(fieldInfo?.FieldType);
            if (!LoogaCatalogDrawer.CanDraw(property, entryType))
            {
                EditorGUILayout.PropertyField(property, GetPropertyLabel(property, metadata), true);
                return true;
            }

            float height = LoogaCatalogDrawer.GetHeight(property, catalogAttribute, entryType);
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            LoogaCatalogDrawer.Draw(rect, property, catalogAttribute, entryType);
            return true;
        }
  
          private bool ShouldDrawNestedFoldout(SerializedProperty property, bool hasCustomDrawer)
        {
            return !hasCustomDrawer
                && property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren;
        }

        private void DrawNestedFoldoutProperty(SerializedProperty property, InspectorPropertyMetadata metadata = null)
        {
            property.isExpanded = LoogaEditorFoldouts.LoogaFoldoutSmall(
                GetPropertyLabel(property, metadata),
                property.isExpanded,
                () =>
                {
                    EditorGUI.indentLevel++;
                    DrawNestedPropertyChildren(property);
                    EditorGUI.indentLevel--;
                },
                property);
        }

        private void DrawFoldoutProperty(SerializedProperty property, LoogaFoldoutAttribute foldoutAttribute, InspectorPropertyMetadata metadata = null)
        {
            string title = string.IsNullOrWhiteSpace(foldoutAttribute.Title)
                ? GetPropertyLabel(property, metadata).text
                : foldoutAttribute.Title;

            string stateKey = GetFoldoutStateKey(property.serializedObject.targetObject.GetType(), property.propertyPath, title);

            if (foldoutAttribute.Style == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaFoldoutLarge(title, stateKey, foldoutAttribute.DefaultExpanded, () =>
                {
                    DrawFoldoutPropertyContent(property, metadata);
                });
                return;
            }

            string initializedKey = $"{stateKey}_Initialized";
            if (!SessionState.GetBool(initializedKey, false))
            {
                property.isExpanded = foldoutAttribute.DefaultExpanded;
                SessionState.SetBool(initializedKey, true);
            }

            property.isExpanded = LoogaEditorFoldouts.LoogaFoldoutSmall(
                PropertyUtils.GetContent(title),
                property.isExpanded,
                () =>
                {
                    DrawFoldoutPropertyContent(property, metadata);
                },
                property);
        }

        private void DrawFoldoutPropertyContent(SerializedProperty property, InspectorPropertyMetadata metadata = null)
        {
            bool hasCustomDrawer = metadata?.hasCustomDrawer ?? CustomDrawerUtil.HasCustomDrawer(property);
            if (!hasCustomDrawer
                && property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren
                && !property.isArray)
            {
                EditorGUI.indentLevel++;
                DrawNestedPropertyChildren(property);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.PropertyField(property, GetPropertyLabel(property, metadata), true);
        }

        private void DrawToggleFoldoutProperty(SerializedProperty property, LoogaToggleFoldoutAttribute toggleFoldoutAttribute, InspectorPropertyMetadata metadata = null)
        {
            string title = string.IsNullOrWhiteSpace(toggleFoldoutAttribute.Title)
                ? GetPropertyLabel(property, metadata).text
                : toggleFoldoutAttribute.Title;

            SerializedProperty toggleProperty = ResolveToggleProperty(property, toggleFoldoutAttribute.TogglePropertyName);
            if (toggleProperty == null)
            {
                DrawFoldoutProperty(property, new LoogaFoldoutAttribute(title, toggleFoldoutAttribute.Style), metadata);
                return;
            }

            string stateKey = GetFoldoutStateKey(property.serializedObject.targetObject.GetType(), property.propertyPath, title);

            if (toggleFoldoutAttribute.Style == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaToggleFoldoutLarge(title, toggleProperty, stateKey, () =>
                {
                    DrawToggleFoldoutPropertyContent(property, toggleProperty, metadata);
                });
                return;
            }

            bool expanded = SessionState.GetBool(stateKey, false);
            bool newExpanded = LoogaEditorFoldouts.LoogaToggleFoldoutSmall(
                PropertyUtils.GetContent(title),
                toggleProperty,
                expanded,
                () => DrawToggleFoldoutPropertyContent(property, toggleProperty, metadata),
                property);

            if (newExpanded != expanded)
                SessionState.SetBool(stateKey, newExpanded);
        }

        private void DrawToggleFoldoutPropertyContent(SerializedProperty property, SerializedProperty toggleProperty, InspectorPropertyMetadata metadata = null)
        {
            bool hasCustomDrawer = metadata?.hasCustomDrawer ?? CustomDrawerUtil.HasCustomDrawer(property);
            if (!hasCustomDrawer
                && property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren
                && !property.isArray)
            {
                EditorGUI.indentLevel++;
                DrawNestedPropertyChildren(property, toggleProperty.propertyPath);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.PropertyField(property, GetPropertyLabel(property, metadata), true);
        }

        private SerializedProperty ResolveToggleProperty(SerializedProperty property, string togglePropertyName)
        {
            if (property.propertyType == SerializedPropertyType.Boolean && string.IsNullOrWhiteSpace(togglePropertyName))
                return property;

            if (string.IsNullOrWhiteSpace(togglePropertyName))
                return null;

            SerializedProperty child = property.FindPropertyRelative(togglePropertyName);
            return child != null && child.propertyType == SerializedPropertyType.Boolean ? child : null;
        }
        private void DrawBoxProperty(SerializedProperty property, LoogaBoxAttribute boxAttribute, InspectorPropertyMetadata metadata = null)
        {
            string title = string.IsNullOrWhiteSpace(boxAttribute.Title)
                ? GetPropertyLabel(property, metadata).text
                : boxAttribute.Title;

            if (boxAttribute.Style == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaBoxLarge(title, () => DrawBoxPropertyContent(property, metadata));
                return;
            }

            LoogaEditorFoldouts.LoogaBoxSmall(PropertyUtils.GetContent(title), () => DrawBoxPropertyContent(property, metadata));
        }

        private void DrawBoxPropertyContent(SerializedProperty property, InspectorPropertyMetadata metadata = null)
        {
            bool hasCustomDrawer = metadata?.hasCustomDrawer ?? CustomDrawerUtil.HasCustomDrawer(property);
            if (!hasCustomDrawer
                && property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren
                && !property.isArray)
            {
                EditorGUI.indentLevel++;
                DrawNestedPropertyChildren(property);
                EditorGUI.indentLevel--;
                return;
            }

            EditorGUILayout.PropertyField(property, GetPropertyLabel(property, metadata), true);
        }

        private void DrawStyledGroup(
            InspectorElement groupStart,
            List<SerializedProperty> groupProperties,
            Type scopeType,
            string basePath)
        {
            if (groupStart.styledGroupIsToggleFoldout)
            {
                DrawToggleFoldoutGroup(groupStart, groupProperties, scopeType, basePath);
                return;
            }

            if (!groupStart.styledGroupIsFoldout)
            {
                DrawBoxGroup(groupStart, groupProperties, scopeType);
                return;
            }

            DrawFoldoutGroup(groupStart, groupProperties, scopeType, basePath);
        }

        private void DrawToggleFoldoutGroup(
            InspectorElement groupStart,
            List<SerializedProperty> groupProperties,
            Type scopeType,
            string basePath)
        {
            if (groupProperties.Count == 0)
                return;

            SerializedProperty toggleProperty = groupProperties[0];
            if (toggleProperty.propertyType != SerializedPropertyType.Boolean)
            {
                DrawFoldoutGroup(groupStart, groupProperties, scopeType, basePath);
                return;
            }

            string title = groupStart.styledGroupName;
            string stateKey = GetFoldoutStateKey(scopeType, $"{basePath}_{title}", title);
            List<SerializedProperty> contentProperties = CopyPropertiesFromIndex(groupProperties, 1);

            if (groupStart.styledGroupStyle == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaToggleFoldoutLarge(title, toggleProperty, stateKey, () =>
                {
                    DrawStyledGroupContent(contentProperties, scopeType);
                });
                return;
            }

            bool expanded = SessionState.GetBool(stateKey, false);
            bool newExpanded = LoogaEditorFoldouts.LoogaToggleFoldoutSmall(PropertyUtils.GetContent(title), toggleProperty, expanded, () =>
            {
                DrawStyledGroupContent(contentProperties, scopeType);
            });

            if (newExpanded != expanded)
                SessionState.SetBool(stateKey, newExpanded);
        }
        private void DrawFoldoutGroup(
            InspectorElement groupStart,
            List<SerializedProperty> groupProperties,
            Type scopeType,
            string basePath)
        {
            string title = groupStart.styledGroupName;
            string stateKey = GetFoldoutStateKey(scopeType, $"{basePath}_{title}", title);

            if (groupStart.styledGroupStyle == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaFoldoutLarge(title, stateKey, groupStart.styledGroupDefaultExpanded, () =>
                {
                    DrawStyledGroupContent(groupProperties, scopeType);
                });
                return;
            }

            bool expanded = SessionState.GetBool(stateKey, groupStart.styledGroupDefaultExpanded);
            bool newExpanded = LoogaEditorFoldouts.LoogaFoldoutSmall(PropertyUtils.GetContent(title), expanded, () =>
            {
                DrawStyledGroupContent(groupProperties, scopeType);
            });

            if (newExpanded != expanded)
                SessionState.SetBool(stateKey, newExpanded);
        }

        private void DrawBoxGroup(
            InspectorElement groupStart,
            List<SerializedProperty> groupProperties,
            Type scopeType)
        {
            string title = groupStart.styledGroupName;

            if (groupStart.styledGroupStyle == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaBoxLarge(title, () => DrawStyledGroupContent(groupProperties, scopeType));
                return;
            }

            LoogaEditorFoldouts.LoogaBoxSmall(PropertyUtils.GetContent(title), () => DrawStyledGroupContent(groupProperties, scopeType));
        }

        private void DrawStyledGroupContent(List<SerializedProperty> groupProperties, Type scopeType)
        {
            InspectorLayout layout = GetLayoutForType(scopeType);

            EditorGUI.indentLevel++;
            for (int i = 0; i < groupProperties.Count; i++)
            {
                SerializedProperty property = groupProperties[i];
                layout.TryGetMetadata(property.name, out InspectorPropertyMetadata metadata);

                if (TryDrawInlineRow(groupProperties, layout, ref i))
                    continue;

                DrawCustomPropertyField(property, metadata);
            }
            EditorGUI.indentLevel--;
        }

        private bool TryDrawInlineRow(List<InspectorElement> elements, List<SerializedProperty> properties, ref int index)
        {
            InspectorElement start = elements[index];
            string rowId = GetInlineRowId(start.metadata);
            if (string.IsNullOrWhiteSpace(rowId))
                return false;

            List<SerializedProperty> rowProperties = new();
            List<GUIContent> rowLabels = new();
            List<float> rowWeights = new();
            int scanIndex = index;

            while (scanIndex < elements.Count)
            {
                InspectorElement element = elements[scanIndex];
                if (element.inStyledGroup || GetInlineRowId(element.metadata) != rowId)
                    break;

                SerializedProperty property = FindSerializedPropertyByName(properties, element.propertyName);
                if (property != null)
                {
                    rowProperties.Add(property);
                    rowLabels.Add(GetPropertyLabel(property, element.metadata));
                    rowWeights.Add(element.metadata?.inlineRowAttribute?.Width ?? 1f);
                }

                scanIndex++;
            }

            if (!DrawInlineRow(rowProperties, rowLabels, rowWeights))
                return false;

            index = scanIndex - 1;
            return true;
        }

        private bool TryDrawInlineRow(List<SerializedProperty> properties, InspectorLayout layout, ref int index)
        {
            SerializedProperty start = properties[index];
            layout.TryGetMetadata(start.name, out InspectorPropertyMetadata startMetadata);
            string rowId = GetInlineRowId(startMetadata);
            if (string.IsNullOrWhiteSpace(rowId))
                return false;

            List<SerializedProperty> rowProperties = new();
            List<GUIContent> rowLabels = new();
            List<float> rowWeights = new();
            int scanIndex = index;

            while (scanIndex < properties.Count)
            {
                SerializedProperty property = properties[scanIndex];
                layout.TryGetMetadata(property.name, out InspectorPropertyMetadata metadata);
                if (GetInlineRowId(metadata) != rowId)
                    break;

                rowProperties.Add(property);
                rowLabels.Add(GetPropertyLabel(property, metadata));
                rowWeights.Add(metadata?.inlineRowAttribute?.Width ?? 1f);
                scanIndex++;
            }

            if (!DrawInlineRow(rowProperties, rowLabels, rowWeights))
                return false;

            index = scanIndex - 1;
            return true;
        }

        private bool DrawInlineRow(List<SerializedProperty> rowProperties, List<GUIContent> rowLabels, List<float> rowWeights)
        {
            if (rowProperties.Count == 0)
                return false;

            SerializedProperty onlyProperty = rowProperties.Count == 1 ? rowProperties[0] : null;
            if (onlyProperty != null
                && onlyProperty.propertyType == SerializedPropertyType.Generic
                && onlyProperty.hasVisibleChildren
                && !onlyProperty.isArray)
            {
                Rect rowRect = EditorGUILayout.GetControlRect(false, InlineRowEditorUtility.SingleLineHeight);
                Rect contentRect = EditorGUI.PrefixLabel(rowRect, rowLabels[0]);
                List<SerializedProperty> childProperties = InlineRowEditorUtility.GetVisibleChildren(onlyProperty);
                List<GUIContent> childLabels = new(childProperties.Count);

                for (int i = 0; i < childProperties.Count; i++)
                    childLabels.Add(PropertyUtils.GetContent(childProperties[i].displayName));

                InlineRowEditorUtility.DrawProperties(contentRect, childProperties, childLabels);
                return true;
            }

            if (rowProperties.Count == 1)
                return false;

            Rect rect = EditorGUILayout.GetControlRect(false, InlineRowEditorUtility.SingleLineHeight);
            InlineRowEditorUtility.DrawProperties(rect, rowProperties, rowLabels, rowWeights);
            return true;
        }

        private static InlineRowAttribute GetStructuredInlineRowAttribute(SerializedProperty property, InspectorPropertyMetadata metadata = null)
        {
            InlineRowAttribute inlineRow = metadata?.inlineRowAttribute ?? PropertyUtils.GetAttribute<InlineRowAttribute>(property);
            return inlineRow ?? CustomDrawerUtil.GetTargetTypeAttribute<InlineRowAttribute>(property);
        }

        private static StructBoxAttribute GetStructuredBoxAttribute(SerializedProperty property)
        {
            StructBoxAttribute structBox = PropertyUtils.GetAttribute<StructBoxAttribute>(property);
            return structBox ?? CustomDrawerUtil.GetTargetTypeAttribute<StructBoxAttribute>(property);
        }

        private static bool TryDrawInlineTypeProperty(SerializedProperty property, GUIContent label)
        {
            if (!CanDrawInlineTypeProperty(property))
                return false;

            Rect rowRect = EditorGUILayout.GetControlRect(false, InlineRowEditorUtility.SingleLineHeight);
            Rect contentRect = IsArrayElement(property) ? rowRect : EditorGUI.PrefixLabel(rowRect, label);
            DrawInlineTypeProperty(contentRect, property);
            return true;
        }

        private static bool TryDrawInlineTypeProperty(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (!CanDrawInlineTypeProperty(property))
                return false;

            Rect contentRect = IsArrayElement(property) ? rect : EditorGUI.PrefixLabel(rect, label);
            DrawInlineTypeProperty(contentRect, property);
            return true;
        }

        private static bool CanDrawInlineTypeProperty(SerializedProperty property)
        {
            return property != null
                && property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren
                && !property.isArray;
        }

        private static void DrawInlineTypeProperty(Rect rect, SerializedProperty property)
        {
            List<SerializedProperty> childProperties = InlineRowEditorUtility.GetVisibleChildren(property);
            List<GUIContent> childLabels = new(childProperties.Count);
            List<float> childWeights = new(childProperties.Count);

            for (int i = 0; i < childProperties.Count; i++)
            {
                SerializedProperty childProperty = childProperties[i];
                InlineRowAttribute childAttribute = PropertyUtils.GetAttribute<InlineRowAttribute>(childProperty);
                childLabels.Add(PropertyUtils.GetLabel(childProperty));
                childWeights.Add(childAttribute?.Width ?? 1f);
            }

            InlineRowEditorUtility.DrawProperties(rect, childProperties, childLabels, childWeights);
        }

        private void DrawStructBoxProperty(SerializedProperty property, StructBoxAttribute structBoxAttribute, InspectorPropertyMetadata metadata = null)
        {
            float height = GetStructBoxPropertyHeight(property);
            Rect rect = EditorGUILayout.GetControlRect(false, height);
            DrawStructBoxProperty(rect, property, structBoxAttribute, GetPropertyLabel(property, metadata));
        }

        private static void DrawStructBoxProperty(Rect position, SerializedProperty property, StructBoxAttribute structBoxAttribute, GUIContent label)
        {
            const float padding = 8f;
            const float headerHeight = 20f;
            const float spacing = 3f;

            GUI.Box(position, GUIContent.none, LoogaEditorFoldouts.SmallBoxStyle);

            Rect headerRect = new(position.x + padding, position.y + 3f, position.width - padding * 2f, headerHeight);
            string title = string.IsNullOrWhiteSpace(structBoxAttribute.Title) ? label.text : structBoxAttribute.Title;
            EditorGUI.LabelField(headerRect, title, EditorStyles.boldLabel);

            Rect contentRect = new(
                position.x + padding,
                headerRect.yMax + spacing,
                position.width - padding * 2f,
                position.height - headerHeight - padding);

            if (!CanDrawInlineTypeProperty(property))
            {
                EditorGUI.PropertyField(contentRect, property, GUIContent.none, true);
                return;
            }

            List<SerializedProperty> children = InlineRowEditorUtility.GetVisibleChildren(property);
            for (int i = 0; i < children.Count; i++)
            {
                SerializedProperty child = children[i];
                float height = GetStructuredPropertyHeight(child);
                Rect childRect = new(contentRect.x, contentRect.y, contentRect.width, height);
                DrawStructuredProperty(childRect, child, PropertyUtils.GetLabel(child));
                contentRect.y += height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        private static void DrawStructuredProperty(Rect rect, SerializedProperty property, GUIContent label)
        {
            InlineRowAttribute inlineRow = GetStructuredInlineRowAttribute(property);
            if (inlineRow != null && TryDrawInlineTypeProperty(rect, property, label))
                return;

            StructBoxAttribute structBox = GetStructuredBoxAttribute(property);
            if (structBox != null)
            {
                DrawStructBoxProperty(rect, property, structBox, label);
                return;
            }

            EditorGUI.PropertyField(rect, property, label, true);
        }

        private static float GetStructuredPropertyHeight(SerializedProperty property)
        {
            if (GetStructuredInlineRowAttribute(property) != null && CanDrawInlineTypeProperty(property))
                return InlineRowEditorUtility.SingleLineHeight;

            StructBoxAttribute structBox = GetStructuredBoxAttribute(property);
            if (structBox != null)
                return GetStructBoxPropertyHeight(property);

            return EditorGUI.GetPropertyHeight(property, true);
        }

        private static float GetStructBoxPropertyHeight(SerializedProperty property)
        {
            const float padding = 8f;
            const float headerHeight = 20f;
            const float spacing = 3f;

            float height = headerHeight + padding + spacing;
            if (!CanDrawInlineTypeProperty(property))
                return height + EditorGUI.GetPropertyHeight(property, true);

            List<SerializedProperty> children = InlineRowEditorUtility.GetVisibleChildren(property);
            for (int i = 0; i < children.Count; i++)
                height += GetStructuredPropertyHeight(children[i]) + EditorGUIUtility.standardVerticalSpacing;

            return height;
        }

        private static bool IsArrayElement(SerializedProperty property)
        {
            return property != null && property.propertyPath.Contains(".Array.data[");
        }

        private void DrawNestedPropertyChildren(SerializedProperty property, string hiddenPropertyPath = null)
        {
            var childProperties = GetNestedSerializedProperties(property);

            if (!string.IsNullOrWhiteSpace(hiddenPropertyPath))
                childProperties.RemoveAll(child => child.propertyPath == hiddenPropertyPath);

            if (TryGetInlineNestedTabType(property, childProperties, out Type nestedType))
            {
                DrawPropertiesScope(childProperties, nestedType, property.propertyPath);
                return;
            }

            foreach (var childProperty in childProperties)
                DrawCustomPropertyField(childProperty);
        }

        private bool TryGetInlineNestedTabType(SerializedProperty property, List<SerializedProperty> childProperties, out Type nestedType)
        {
            nestedType = CustomDrawerUtil.GetTargetType(property);

            if (nestedType == null)
                return false;

            InspectorLayout nestedLayout = GetLayoutForType(nestedType);
            return nestedLayout.HasTabs
                && childProperties.Count > 0
                && LayoutContainsAllProperties(nestedLayout, childProperties);
        }

        private void DrawButtons(InspectorLayout layout, bool drawTop)
        {
            bool hasMatchingButton = false;
            for (int i = 0; i < layout.buttons.Count; i++)
            {
                if (layout.buttons[i].drawAtTop == drawTop)
                {
                    hasMatchingButton = true;
                    break;
                }
            }

            if (!hasMatchingButton)
                return;

            EditorGUILayout.Space(2f);

            bool drewButton = false;
            for (int i = 0; i < layout.buttons.Count; i++)
            {
                InspectorButton button = layout.buttons[i];
                if (button.drawAtTop != drawTop)
                    continue;

                if (drewButton)
                    EditorGUILayout.Space(2f);

                DrawButton(button);
                drewButton = true;
            }

            if (drawTop)
                EditorGUILayout.Space(2f);
        }

        private void DrawButton(InspectorButton button)
        {
            bool enabled = IsButtonEnabled(button);

            using (new EditorGUI.DisabledScope(!enabled))
            {
                if (!GUILayout.Button(button.label, GUILayout.Height(button.height)))
                    return;

                if (!ShouldInvokeButton(button))
                    return;

                for (int i = 0; i < targets.Length; i++)
                    button.method.Invoke(targets[i], null);
            }
        }

        private const float ListHeaderHeight = 23f;
        private const float ListHeaderArrowSize = 10.5f;
        private const float ListHeaderAccentWidth = 4f;
        private const float ListHeaderLeftInset = 6f;
        private const float ListHeaderTextArrowGap = 6f;
        private const float ListHeaderButtonSize = 18f;
        private const float ListHeaderButtonGap = 2f;
        private const float ListSizeFieldWidth = 48f;
        private const float ListSizeFieldRightPadding = 8f;
        private const float ListBodyPaddingX = 7f;
        private const float ListBodyPaddingY = 5f;
        private const float ListRowPaddingX = 7f;
        private const float ListRowPaddingY = 3f;
        private const float ListRowGap = 1f;
        private const float ListDragHandleWidth = 16f;
        private const float ListRowDeleteWidth = 20f;
        private const float ListEmptyRowHeight = 22f;

        private void DrawLoogaList(SerializedProperty property)
        {
            FieldInfo field = ReflectionUtils.GetField(target.GetType(), property.name);
            DrawListValidation(property, field);

            Event e = Event.current;
            string key = property.propertyPath;
            Rect headerRect = EditorGUILayout.GetControlRect(false, ListHeaderHeight);
            Rect boxRect = new(headerRect.x - 3f, headerRect.y, headerRect.width + 6f, headerRect.height);
            float headerControlY = Mathf.Round(CenterVertically(boxRect, EditorGUIUtility.singleLineHeight).y);
            Rect sizeRect = new(
                boxRect.xMax - ListSizeFieldWidth - ListHeaderButtonSize * 2f - ListHeaderButtonGap * 2f - ListSizeFieldRightPadding,
                headerControlY,
                ListSizeFieldWidth,
                EditorGUIUtility.singleLineHeight);
            Rect addRect = new(
                sizeRect.xMax + ListHeaderButtonGap,
                sizeRect.y,
                ListHeaderButtonSize,
                sizeRect.height);
            Rect removeRect = new(
                addRect.xMax + ListHeaderButtonGap,
                sizeRect.y,
                ListHeaderButtonSize,
                sizeRect.height);
            Rect toggleRect = new(
                boxRect.x,
                boxRect.y,
                Mathf.Max(0f, sizeRect.x - boxRect.x - ListSizeFieldRightPadding),
                boxRect.height);

            float bodyHeight = property.isExpanded ? GetListBodyHeight(property) : 0f;
            Rect fullRect = new(boxRect.x, boxRect.y, boxRect.width, boxRect.height + bodyHeight);

            EditorGUIUtility.AddCursorRect(toggleRect, MouseCursor.Arrow);
            HandleListDragAndDrop(property, fullRect, field);
            DrawListHeaderBackground(boxRect, toggleRect);

            bool isExpanded = property.isExpanded;
            if (e.type == EventType.MouseDown && toggleRect.Contains(e.mousePosition) && e.button == 0)
            {
                property.isExpanded = !property.isExpanded;
                isExpanded = property.isExpanded;
                e.Use();
            }

            Rect arrowRect = new(
                boxRect.x + ListHeaderLeftInset + ListHeaderAccentWidth,
                CenterVertically(boxRect, ListHeaderArrowSize).y,
                ListHeaderArrowSize,
                ListHeaderArrowSize);
            Rect labelRect = new(
                arrowRect.xMax + ListHeaderTextArrowGap,
                boxRect.y + 1f,
                Mathf.Max(0f, toggleRect.xMax - arrowRect.xMax - ListHeaderTextArrowGap - ListHeaderLeftInset),
                boxRect.height);

            DrawListFoldoutArrow(arrowRect, isExpanded);
            EditorGUI.LabelField(labelRect, PropertyUtils.GetLabel(property), EditorStyles.label);

            EditorGUI.BeginChangeCheck();
            int newSize = Mathf.Max(0, EditorGUI.DelayedIntField(sizeRect, property.arraySize));
            if (EditorGUI.EndChangeCheck())
            {
                property.arraySize = newSize;
                ClampListSelection(key, property.arraySize);
            }

            DrawListHeaderButtons(property, key, addRect, removeRect);

            if (!property.isExpanded)
            {
                CancelListDrag(key);
                return;
            }

            float expandedBodyHeight = GetListBodyHeight(property);
            GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Height(expandedBodyHeight), GUILayout.ExpandWidth(true));
            Rect bodyRect = new(headerRect.x - 3f, boxRect.yMax, headerRect.width + 6f, expandedBodyHeight);
            DrawListBody(property, key, bodyRect);
        }

        private void DrawListValidation(SerializedProperty property, FieldInfo field)
        {
            if (field == null)
                return;

            if (Attribute.GetCustomAttribute(field, typeof(ValidateInputAttribute)) is not ValidateInputAttribute valInputAttr)
                return;

            bool condition = ValidateInputDrawer.GetCondition(target, valInputAttr.condition);
            if (!condition)
                return;

            MessageType msgType = ValidateInputDrawer.GetMessageType(valInputAttr.messageMode);
            EditorGUILayout.HelpBox(valInputAttr.message, msgType);
        }

        private void DrawListHeaderButtons(SerializedProperty property, string key, Rect addRect, Rect removeRect)
        {
            if (GUI.Button(addRect, new GUIContent("+", "Add item"), EditorStyles.miniButtonLeft))
                AddListElement(property, key);

            using (new EditorGUI.DisabledScope(!HasListSelection(key)))
            {
                if (GUI.Button(removeRect, new GUIContent("-", "Remove selected items"), EditorStyles.miniButtonRight))
                    DeleteSelectedListElements(property, key);
            }
        }

        private void DrawListBody(SerializedProperty property, string key, Rect bodyRect)
        {
            Event e = Event.current;
            float listBoxHeight = GetListRowsHeight(property) + ListBodyPaddingY * 2f;
            Rect listBoxRect = new(bodyRect.x, bodyRect.y, bodyRect.width, listBoxHeight);
            GUI.Box(listBoxRect, GUIContent.none, LoogaEditorFoldouts.SmallBoxStyle);

            Rect contentRect = new(
                listBoxRect.x + ListHeaderAccentWidth + ListBodyPaddingX,
                listBoxRect.y + ListBodyPaddingY,
                Mathf.Max(0f, listBoxRect.width - ListHeaderAccentWidth - ListBodyPaddingX * 2f),
                Mathf.Max(0f, listBoxRect.height - ListBodyPaddingY * 2f));

            HandleListDragOver(property, key, contentRect);
            bool draggingThisList = _draggingListKey == key && _draggingListIndex >= 0 && _draggingListIndex < property.arraySize;
            int dropIndex = draggingThisList ? Mathf.Clamp(_draggingListDropIndex, 0, property.arraySize) : -1;
            SerializedProperty draggedElement = null;
            float draggedElementHeight = 0f;
            float draggedRowHeight = 0f;
            float y = contentRect.y;

            if (property.arraySize == 0)
            {
                Rect emptyRect = new(contentRect.x, y, contentRect.width, ListEmptyRowHeight);
                DrawListRowBackground(emptyRect, false, emptyRect.Contains(e.mousePosition), false);
                EditorGUI.LabelField(emptyRect, "Empty", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                for (int i = 0; i < property.arraySize; i++)
                {
                    SerializedProperty element = property.GetArrayElementAtIndex(i);
                    float elementHeight = GetStructuredPropertyHeight(element);
                    float rowHeight = elementHeight + ListRowPaddingY * 2f;

                    if (draggingThisList && i == _draggingListIndex)
                    {
                        draggedElement = element;
                        draggedElementHeight = elementHeight;
                        draggedRowHeight = rowHeight;
                    }

                    if (draggingThisList && i == dropIndex)
                        y += draggedRowHeight + ListRowGap;

                    if (draggingThisList && i == _draggingListIndex)
                        continue;

                    Rect rowRect = new(contentRect.x, y, contentRect.width, rowHeight);
                    if (HandleListRowInput(key, rowRect, i))
                        return;

                    bool selected = IsListRowSelected(key, i);
                    bool hovered = rowRect.Contains(e.mousePosition);
                    DrawListRowBackground(rowRect, selected, hovered, false);
                    DrawListRow(property, key, rowRect, element, elementHeight, i);
                    y = rowRect.yMax + ListRowGap;
                }

                if (draggingThisList && dropIndex == property.arraySize)
                    y += draggedRowHeight + ListRowGap;

                if (draggingThisList && draggedElement != null)
                {
                    Rect draggedRowRect = new(contentRect.x, e.mousePosition.y - _draggingListMouseOffsetY, contentRect.width, draggedRowHeight);
                    DrawListRowBackground(draggedRowRect, true, false, true);
                    DrawListRow(property, key, draggedRowRect, draggedElement, draggedElementHeight, _draggingListIndex);
                }
            }
        }

        private void DrawListRow(SerializedProperty property, string key, Rect rowRect, SerializedProperty element, float elementHeight, int index)
        {
            float deleteHeight = EditorGUIUtility.singleLineHeight;
            Rect deleteRect = new(
                0f,
                CenterVertically(rowRect, deleteHeight).y,
                ListRowDeleteWidth,
                deleteHeight);
            float deleteRightInset = deleteRect.y - rowRect.y;
            deleteRect.x = rowRect.xMax - deleteRightInset - deleteRect.width;

            Rect dragRect = new(rowRect.x + ListRowPaddingX, rowRect.y, ListDragHandleWidth, rowRect.height);
            Rect elementRect = new(
                dragRect.xMax + ListRowPaddingX,
                rowRect.y + ListRowPaddingY,
                Mathf.Max(0f, deleteRect.x - dragRect.xMax - ListRowPaddingX * 2f),
                elementHeight);

            DrawListDragHandle(dragRect);
            DrawListElement(elementRect, element);

            if (GUI.Button(deleteRect, new GUIContent("-", "Remove item"), EditorStyles.miniButton))
            {
                DeleteListElement(property, index);
                ShiftListSelectionAfterDelete(key, index, property.arraySize);
                GUI.changed = true;
            }
        }

        private bool HandleListRowInput(string key, Rect rowRect, int index)
        {
            Event e = Event.current;
            Rect dragRect = new(rowRect.x + ListRowPaddingX, rowRect.y, ListDragHandleWidth, rowRect.height);

            if (e.type == EventType.MouseDown && e.button == 0 && rowRect.Contains(e.mousePosition))
            {
                SelectListRow(key, index, e);

                if (dragRect.Contains(e.mousePosition))
                {
                    _draggingListKey = key;
                    _draggingListIndex = index;
                    _draggingListDropIndex = index;
                    _draggingListMouseOffsetY = e.mousePosition.y - rowRect.y;
                    e.Use();
                }
            }

            return false;
        }

        private void HandleListDragOver(SerializedProperty property, string key, Rect contentRect)
        {
            Event e = Event.current;
            if (_draggingListKey != key || _draggingListIndex < 0)
                return;

            if (e.type == EventType.MouseDrag)
            {
                _draggingListDropIndex = GetListDropIndex(property, contentRect, e.mousePosition.y);
                GUI.changed = true;
                e.Use();
                return;
            }

            if (e.type != EventType.MouseUp)
                return;

            CommitListDrag(property, key);
            e.Use();
        }

        private void CommitListDrag(SerializedProperty property, string key)
        {
            int sourceIndex = _draggingListIndex;
            int dropIndex = Mathf.Clamp(_draggingListDropIndex, 0, property.arraySize);
            CancelListDrag(key);

            if (sourceIndex < 0 || sourceIndex >= property.arraySize)
                return;

            if (dropIndex == sourceIndex || dropIndex == sourceIndex + 1)
                return;

            int targetIndex = dropIndex > sourceIndex ? dropIndex - 1 : dropIndex;
            property.MoveArrayElement(sourceIndex, targetIndex);
            SelectOnlyListRow(key, targetIndex);
            GUI.changed = true;
        }

        private void DrawListElement(Rect rect, SerializedProperty element)
        {
            int cachedIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            DrawStructuredProperty(rect, element, PropertyUtils.GetContent(element.displayName));
            EditorGUI.indentLevel = cachedIndent;
        }

        private static void DrawListHeaderBackground(Rect boxRect, Rect toggleRect)
        {
            GUI.Box(boxRect, GUIContent.none, LoogaEditorFoldouts.SmallFoldoutBoxStyle);

            if (toggleRect.Contains(Event.current.mousePosition))
                LoogaEditorFoldouts.DrawHoverRect(boxRect);
        }

        private static void DrawListRowBackground(Rect rect, bool selected, bool hovered, bool dragging)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color color = GetListRowColor();
            if (hovered)
                color = Color.Lerp(color, GetListHoverColor(), 0.65f);
            if (selected)
                color = Color.Lerp(color, GetListSelectionColor(), 0.32f);
            if (dragging)
                color = Color.Lerp(color, GetListSelectionColor(), 0.55f);

            EditorGUI.DrawRect(rect, color);
        }

        private static void DrawListDragHandle(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color lineColor = EditorGUIUtility.isProSkin
                ? new Color(0.48f, 0.48f, 0.48f, 1f)
                : new Color(0.36f, 0.36f, 0.36f, 1f);
            float centerX = Mathf.Round(rect.x + 5f);
            float centerY = Mathf.Round(rect.center.y - 1f);

            for (int i = -1; i <= 1; i++)
            {
                Rect lineRect = new(centerX - 4f, centerY + i * 4f, 8f, 1f);
                EditorGUI.DrawRect(lineRect, lineColor);
            }
        }
        private static void DrawListFoldoutArrow(Rect rect, bool expanded)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Color previousColor = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.68f, 0.68f, 0.68f, 1f)
                : new Color(0.28f, 0.28f, 0.28f, 1f);

            Vector2 center = rect.center;
            float radius = ListHeaderArrowSize * 0.5f;
            float verticalRadius = radius * Mathf.Sqrt(3f) * 0.5f;
            Vector3[] points = expanded
                ? new[]
                {
                    new Vector3(center.x - radius, center.y - verticalRadius * 0.75f, 0f),
                    new Vector3(center.x + radius, center.y - verticalRadius * 0.75f, 0f),
                    new Vector3(center.x, center.y + verticalRadius * 0.75f, 0f)
                }
                : new[]
                {
                    new Vector3(center.x - verticalRadius * 0.5f, center.y - radius, 0f),
                    new Vector3(center.x - verticalRadius * 0.5f, center.y + radius, 0f),
                    new Vector3(center.x + verticalRadius, center.y, 0f)
                };

            Handles.BeginGUI();
            Handles.DrawAAConvexPolygon(points);
            Handles.EndGUI();
            Handles.color = previousColor;
        }

        private static float GetListBodyHeight(SerializedProperty property)
        {
            return ListBodyPaddingY * 2f + GetListRowsHeight(property);
        }

        private static float GetListRowsHeight(SerializedProperty property)
        {
            if (property.arraySize == 0)
                return ListEmptyRowHeight;

            float height = 0f;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                height += GetStructuredPropertyHeight(element) + ListRowPaddingY * 2f;

                if (i < property.arraySize - 1)
                    height += ListRowGap;
            }

            return height;
        }

        private static int GetListDropIndex(SerializedProperty property, Rect contentRect, float mouseY)
        {
            if (property.arraySize == 0)
                return 0;

            float y = contentRect.y;
            for (int i = 0; i < property.arraySize; i++)
            {
                float rowHeight = GetStructuredPropertyHeight(property.GetArrayElementAtIndex(i)) + ListRowPaddingY * 2f;
                if (mouseY < y + rowHeight * 0.5f)
                    return i;

                y += rowHeight + ListRowGap;
            }

            return property.arraySize;
        }
        private HashSet<int> GetListSelection(string key)
        {
            if (_listSelectedIndices.TryGetValue(key, out HashSet<int> selection))
                return selection;

            selection = new HashSet<int>();
            _listSelectedIndices[key] = selection;
            return selection;
        }

        private bool HasListSelection(string key)
        {
            return _listSelectedIndices.TryGetValue(key, out HashSet<int> selection) && selection.Count > 0;
        }

        private bool IsListRowSelected(string key, int index)
        {
            return _listSelectedIndices.TryGetValue(key, out HashSet<int> selection) && selection.Contains(index);
        }

        private void SelectListRow(string key, int index, Event e)
        {
            HashSet<int> selection = GetListSelection(key);
            bool additive = EditorGUI.actionKey;
            if (e.shift && _listSelectionAnchors.TryGetValue(key, out int anchor))
            {
                selection.Clear();
                int start = Mathf.Min(anchor, index);
                int end = Mathf.Max(anchor, index);
                for (int i = start; i <= end; i++)
                    selection.Add(i);
                return;
            }

            if (additive)
            {
                if (!selection.Add(index))
                    selection.Remove(index);
                _listSelectionAnchors[key] = index;
                return;
            }

            SelectOnlyListRow(key, index);
        }

        private void SelectOnlyListRow(string key, int index)
        {
            HashSet<int> selection = GetListSelection(key);
            selection.Clear();
            selection.Add(index);
            _listSelectionAnchors[key] = index;
        }

        private void ClampListSelection(string key, int arraySize)
        {
            if (!_listSelectedIndices.TryGetValue(key, out HashSet<int> selection))
                return;

            if (arraySize <= 0)
            {
                selection.Clear();
                _listSelectionAnchors.Remove(key);
                return;
            }

            selection.RemoveWhere(index => index < 0 || index >= arraySize);
            if (_listSelectionAnchors.TryGetValue(key, out int anchor))
                _listSelectionAnchors[key] = Mathf.Clamp(anchor, 0, arraySize - 1);
        }

        private void AddListElement(SerializedProperty property, string key)
        {
            property.arraySize++;
            SelectOnlyListRow(key, property.arraySize - 1);
            GUI.changed = true;
        }

        private void DeleteSelectedListElements(SerializedProperty property, string key)
        {
            if (!_listSelectedIndices.TryGetValue(key, out HashSet<int> selection) || selection.Count == 0)
                return;

            int[] indices = selection.Where(index => index >= 0 && index < property.arraySize).OrderByDescending(index => index).ToArray();
            for (int i = 0; i < indices.Length; i++)
                DeleteListElement(property, indices[i]);

            selection.Clear();
            _listSelectionAnchors.Remove(key);
            ClampListSelection(key, property.arraySize);
            GUI.changed = true;
        }

        private void ShiftListSelectionAfterDelete(string key, int deletedIndex, int arraySize)
        {
            if (!_listSelectedIndices.TryGetValue(key, out HashSet<int> selection))
                return;

            int[] shifted = selection
                .Where(index => index != deletedIndex)
                .Select(index => index > deletedIndex ? index - 1 : index)
                .Where(index => index >= 0 && index < arraySize)
                .ToArray();

            selection.Clear();
            for (int i = 0; i < shifted.Length; i++)
                selection.Add(shifted[i]);

            if (_listSelectionAnchors.TryGetValue(key, out int anchor))
            {
                if (anchor == deletedIndex)
                    _listSelectionAnchors.Remove(key);
                else
                    _listSelectionAnchors[key] = Mathf.Clamp(anchor > deletedIndex ? anchor - 1 : anchor, 0, Mathf.Max(0, arraySize - 1));
            }
        }

        private static void DeleteListElement(SerializedProperty property, int index)
        {
            if (index < 0 || index >= property.arraySize)
                return;

            SerializedProperty element = property.GetArrayElementAtIndex(index);
            bool deleteAgain = element.propertyType == SerializedPropertyType.ObjectReference && element.objectReferenceValue != null;
            property.DeleteArrayElementAtIndex(index);

            if (deleteAgain)
                property.DeleteArrayElementAtIndex(index);
        }

        private void CancelListDrag(string key)
        {
            if (_draggingListKey != key)
                return;

            _draggingListKey = string.Empty;
            _draggingListIndex = -1;
            _draggingListDropIndex = -1;
            _draggingListMouseOffsetY = 0f;
        }

        private static Color GetListRowColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.255f, 0.255f, 0.255f, 1f)
                : new Color(0.76f, 0.76f, 0.76f, 1f);
        }

        private static Color GetListHoverColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.30f, 0.30f, 0.30f, 1f)
                : new Color(0.82f, 0.82f, 0.82f, 1f);
        }

        private static Color GetListSelectionColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.18f, 0.42f, 0.72f, 1f)
                : new Color(0.28f, 0.55f, 0.90f, 1f);
        }
        private static Rect CenterVertically(Rect rect, float height)
        {
            return new Rect(rect.x, rect.y + Mathf.Max(0f, (rect.height - height) * 0.5f), rect.width, height);
        }
        #endregion
        
        #region Getters
        private List<SerializedProperty> GetSerializedProperties()
        {
            List<SerializedProperty> serializedProperties = new List<SerializedProperty>();

            using SerializedProperty iterator = serializedObject.GetIterator();
            
            //get visible properties
            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.name != "m_Script")
                        serializedProperties.Add(iterator.Copy());
                } while (iterator.NextVisible(false));
            }
            
            return serializedProperties;
        }

        private List<SerializedProperty> GetNestedSerializedProperties(SerializedProperty property)
        {
            List<SerializedProperty> serializedProperties = new List<SerializedProperty>();
            
            SerializedProperty iterator = property.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();
            int parentDepth = iterator.depth;

            if (iterator.NextVisible(true))
            {
                do
                {
                    if (iterator.depth <= parentDepth || SerializedProperty.EqualContents(iterator, endProperty))
                        break;
                    
                    serializedProperties.Add(iterator.Copy());
                } while (iterator.NextVisible(false));
            }
            
            return serializedProperties;
        }

        private InspectorLayout GetLayoutForType(Type type)
        {
            if (_layoutCache.TryGetValue(type, out var layout))
                return layout;

            layout = new InspectorLayout();

            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = type.GetFields(bindingFlags);
            
            TabGroupDefinition currentGroup = null;
            bool inTabGroup = false;
            List<string> currentTabPath = new();
            string currentStyledGroupName = null;
            LoogaFoldoutStyle currentStyledGroupStyle = LoogaFoldoutStyle.Small;
            bool currentStyledGroupDefaultExpanded = true;
            bool currentStyledGroupIsFoldout = true;
            bool currentStyledGroupIsToggleFoldout = false;

            foreach (var field in fields)
            {
                var tabAttributes = field.GetCustomAttributes<TabAttribute>()
                    .OrderBy(attribute => attribute.level)
                    .ToArray();
                var tabEndAttribute = field.GetCustomAttribute<TabEndAttribute>();
                var foldoutGroupAttribute = field.GetCustomAttribute<LoogaFoldoutGroupAttribute>();
                var foldoutGroupEndAttribute = field.GetCustomAttribute<LoogaFoldoutGroupEndAttribute>();
                var boxGroupAttribute = field.GetCustomAttribute<LoogaBoxGroupAttribute>();
                var boxGroupEndAttribute = field.GetCustomAttribute<LoogaBoxGroupEndAttribute>();
                var toggleFoldoutGroupAttribute = field.GetCustomAttribute<LoogaToggleFoldoutGroupAttribute>();
                var toggleFoldoutGroupEndAttribute = field.GetCustomAttribute<LoogaToggleFoldoutGroupEndAttribute>();

                if (tabAttributes.Length > 0)
                {
                    if (!inTabGroup)
                    {
                        inTabGroup = true;
                        currentGroup = new TabGroupDefinition();
                        layout.tabGroups.Add(currentGroup);
                    }

                    foreach (TabAttribute tabAttribute in tabAttributes)
                        ApplyTabAttribute(currentTabPath, tabAttribute);

                    currentGroup?.AddPath(currentTabPath);
                }
                else
                {
                    if (tabEndAttribute != null)
                    {
                        inTabGroup = false;
                        currentGroup = null;
                        currentTabPath.Clear();
                    }
                }

                InspectorElement currentElement = inTabGroup
                    ? new InspectorElement(field.Name, currentTabPath)
                    : new InspectorElement(field.Name);

                if (toggleFoldoutGroupAttribute != null)
                {
                    currentStyledGroupName = toggleFoldoutGroupAttribute.Title;
                    currentStyledGroupStyle = toggleFoldoutGroupAttribute.Style;
                    currentStyledGroupDefaultExpanded = false;
                    currentStyledGroupIsFoldout = true;
                    currentStyledGroupIsToggleFoldout = true;
                }
                else if (foldoutGroupAttribute != null)
                {
                    currentStyledGroupName = foldoutGroupAttribute.Title;
                    currentStyledGroupStyle = foldoutGroupAttribute.Style;
                    currentStyledGroupDefaultExpanded = foldoutGroupAttribute.DefaultExpanded;
                    currentStyledGroupIsFoldout = true;
                    currentStyledGroupIsToggleFoldout = false;
                }
                else if (boxGroupAttribute != null)
                {
                    currentStyledGroupName = boxGroupAttribute.Title;
                    currentStyledGroupStyle = boxGroupAttribute.Style;
                    currentStyledGroupDefaultExpanded = true;
                    currentStyledGroupIsFoldout = false;
                    currentStyledGroupIsToggleFoldout = false;
                }

                bool inStyledGroup = !string.IsNullOrWhiteSpace(currentStyledGroupName);
                if (currentStyledGroupIsToggleFoldout)
                {
                    currentElement.SetToggleFoldoutGroup(
                        currentStyledGroupName,
                        currentStyledGroupStyle,
                        toggleFoldoutGroupEndAttribute != null);
                }
                else if (currentStyledGroupIsFoldout)
                {
                    currentElement.SetFoldoutGroup(
                        currentStyledGroupName,
                        currentStyledGroupStyle,
                        currentStyledGroupDefaultExpanded,
                        foldoutGroupEndAttribute != null);
                }
                else
                {
                    currentElement.SetBoxGroup(
                        currentStyledGroupName,
                        currentStyledGroupStyle,
                        boxGroupEndAttribute != null);
                }

                InspectorPropertyMetadata metadata = InspectorPropertyMetadata.Create(field);
                currentElement.SetMetadata(metadata);

                layout.elements.Add(currentElement);
                layout.propertyNames.Add(currentElement.propertyName);
                layout.propertyMetadata[currentElement.propertyName] = metadata;

                if (inStyledGroup && (foldoutGroupEndAttribute != null || boxGroupEndAttribute != null || toggleFoldoutGroupEndAttribute != null))
                {
                    currentStyledGroupName = null;
                    currentStyledGroupStyle = LoogaFoldoutStyle.Small;
                    currentStyledGroupDefaultExpanded = true;
                    currentStyledGroupIsFoldout = true;
                    currentStyledGroupIsToggleFoldout = false;
                }
            }
            
            var methodFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            var methods = type.GetMethods(methodFlags);

            foreach (var m in methods)
            {
                var buttonAttribute = m.GetCustomAttribute<ButtonAttribute>();
                if (buttonAttribute == null)
                    continue;
                
                string buttonLabel = string.IsNullOrEmpty(buttonAttribute.label) ? ObjectNames.NicifyVariableName(m.Name) : buttonAttribute.label;
                
                layout.buttons.Add(new InspectorButton
                {
                    method = m,
                    label = buttonLabel,
                    drawAtTop = buttonAttribute.drawAtTop,
                    enableIf = buttonAttribute.enableIf,
                    confirmMessage = buttonAttribute.confirmMessage,
                    height = Mathf.Max(1f, buttonAttribute.height),
                    mode = buttonAttribute.mode
                });
            }
            
            _layoutCache[type] = layout;
            return layout;
        }
        
        #endregion
        
        #region Helpers
        private static GUIContent GetPropertyLabel(SerializedProperty property, InspectorPropertyMetadata metadata)
        {
            return metadata?.label ?? PropertyUtils.GetLabel(property);
        }
        private static LoogaInspectorMessageAttribute[] GetInspectorMessages(Type inspectedType)
        {
            if (_messageCache.TryGetValue(inspectedType, out LoogaInspectorMessageAttribute[] messages))
                return messages;

            messages = inspectedType.GetCustomAttributes<LoogaInspectorMessageAttribute>(inherit: true).ToArray();
            _messageCache[inspectedType] = messages;
            return messages;
        }

        private static StatusBoxAttribute[] GetStatusBoxes(Type inspectedType)
        {
            if (_statusBoxCache.TryGetValue(inspectedType, out StatusBoxAttribute[] statusBoxes))
                return statusBoxes;

            statusBoxes = inspectedType.GetCustomAttributes<StatusBoxAttribute>(inherit: true).ToArray();
            _statusBoxCache[inspectedType] = statusBoxes;
            return statusBoxes;
        }

        private static OpenEditorWindowAttribute[] GetOpenEditorWindowAttributes(Type inspectedType)
        {
            if (_openWindowCache.TryGetValue(inspectedType, out OpenEditorWindowAttribute[] openWindows))
                return openWindows;

            openWindows = inspectedType.GetCustomAttributes<OpenEditorWindowAttribute>(inherit: true).ToArray();
            _openWindowCache[inspectedType] = openWindows;
            return openWindows;
        }

        private static string GetInlineRowId(InspectorPropertyMetadata metadata)
        {
            InlineRowAttribute inlineRow = metadata?.inlineRowAttribute;
            if (inlineRow == null)
                return null;

            return string.IsNullOrWhiteSpace(inlineRow.RowId)
                ? metadata.propertyName
                : inlineRow.RowId;
        }
        private bool IsButtonEnabled(InspectorButton button)
        {
            if (button.mode == LoogaButtonMode.EditModeOnly && EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            if (button.mode == LoogaButtonMode.PlayModeOnly && !EditorApplication.isPlayingOrWillChangePlaymode)
                return false;

            if (string.IsNullOrWhiteSpace(button.enableIf))
                return true;

            for (int i = 0; i < targets.Length; i++)
            {
                if (PropertyUtils.GetConditionValue(targets[i], button.enableIf))
                    return true;
            }

            return false;
        }

        private static bool ShouldInvokeButton(InspectorButton button)
        {
            if (string.IsNullOrWhiteSpace(button.confirmMessage))
                return true;

            return EditorUtility.DisplayDialog(
                button.label,
                button.confirmMessage,
                "Confirm",
                "Cancel");
        }

        private static SerializedProperty FindSerializedPropertyByName(List<SerializedProperty> properties, string propertyName)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].name == propertyName)
                    return properties[i];
            }

            return null;
        }

        private static List<SerializedProperty> CopyPropertiesFromIndex(List<SerializedProperty> properties, int startIndex)
        {
            List<SerializedProperty> copiedProperties = new();
            for (int i = startIndex; i < properties.Count; i++)
                copiedProperties.Add(properties[i]);

            return copiedProperties;
        }

        private static bool LayoutContainsAllProperties(InspectorLayout layout, List<SerializedProperty> properties)
        {
            for (int i = 0; i < properties.Count; i++)
            {
                if (!LayoutContainsProperty(layout, properties[i].name))
                    return false;
            }

            return true;
        }

        private static bool LayoutContainsProperty(InspectorLayout layout, string propertyName)
        {
            for (int i = 0; i < layout.elements.Count; i++)
            {
                if (layout.elements[i].propertyName == propertyName)
                    return true;
            }

            return false;
        }
        private static string GetTabStateKey(Type scopeType, string basePath, int tabGroupIndex)
        {
            string typeKey = scopeType != null ? scopeType.FullName : "UnknownType";
            return $"{typeKey}_{basePath}_{tabGroupIndex}_tab";
        }

        private static string GetFoldoutStateKey(Type scopeType, string basePath, string title)
        {
            string typeKey = scopeType != null ? scopeType.FullName : "UnknownType";
            return $"{typeKey}_{basePath}_{title}_foldout";
        }

        private void HandleListDragAndDrop(SerializedProperty property, Rect dropArea, FieldInfo fieldInfo)
        {
            //validate mouse position/action
            Event e = Event.current;
            if (!dropArea.Contains(e.mousePosition) || (e.type != EventType.DragUpdated && e.type != EventType.DragPerform))
                return;
            //validate field info
            if (fieldInfo == null)
                return;

            //get type for array, list, etc.
            Type elementType = null;
            if (fieldInfo.FieldType.IsArray)
                elementType = fieldInfo.FieldType.GetElementType();
            else if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                elementType = fieldInfo.FieldType.GetGenericArguments()[0];
            
            //return if interface or null
            if (elementType == null || (!typeof(Object).IsAssignableFrom(elementType) && elementType.IsInterface))
                return;
            
            // Filter dragged objects to the list element type once, then reuse the resolved objects on drop.
            List<Object> validReferences = new();
            for (int i = 0; i < DragAndDrop.objectReferences.Length; i++)
            {
                Object reference = DragAndDrop.objectReferences[i];
                if (reference == null)
                    continue;

                if (reference is GameObject gameObject && typeof(Component).IsAssignableFrom(elementType))
                {
                    Component component = gameObject.GetComponent(elementType);
                    if (component != null)
                        validReferences.Add(component);

                    continue;
                }

                if (elementType.IsInstanceOfType(reference))
                    validReferences.Add(reference);
            }

            if (validReferences.Count == 0)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                for (int i = 0; i < validReferences.Count; i++)
                {
                    property.arraySize++;
                    property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = validReferences[i];
                }
                
                //mark serialized property as changed
                GUI.changed = true;
                e.Use();
            }
        }
        #endregion
    }
}







