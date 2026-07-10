using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    [CustomEditor(typeof(Object), true)]
    [CanEditMultipleObjects]
    public class LoogaEditor : UnityEditor.Editor
    {
        private readonly Dictionary<string, ReorderableList> _reorderableLists = new();
        private static readonly Dictionary<Type, InspectorLayout> _layoutCache = new();
        private static readonly Dictionary<Type, LoogaInspectorMessageAttribute[]> _messageCache = new();
        private static readonly Dictionary<Type, StatusBoxAttribute[]> _statusBoxCache = new();
        private static readonly Dictionary<Type, OpenEditorWindowAttribute[]> _openWindowCache = new();
        
        #region Built-In
        private void OnDisable()
        {
            _reorderableLists.Clear();
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
                EditorGUILayout.Space(1f);
            }
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
                    DrawReorderableList(property);
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

        private void DrawReorderableList(SerializedProperty property)
        {
            FieldInfo field = ReflectionUtils.GetField(target.GetType(), property.name);
            if (field != null)
            {
                if (Attribute.GetCustomAttribute(field, typeof(ValidateInputAttribute)) is ValidateInputAttribute valInputAttr)
                {
                    bool condition = ValidateInputDrawer.GetCondition(target, valInputAttr.condition);
                    if (condition)
                    {
                        MessageType msgType = ValidateInputDrawer.GetMessageType(valInputAttr.messageMode);
                        EditorGUILayout.HelpBox(valInputAttr.message, msgType);
                    }
                }
            }
            
            Event e = Event.current;
            string key = property.propertyPath;

            if (!_reorderableLists.TryGetValue(key, out ReorderableList list))
            {
                list = new ReorderableList(property.serializedObject, property, true, false, true, true)
                    {
                        headerHeight = 0f,
                        
                        drawElementCallback = (rect, index, isActive, _) =>
                        {
                            int cachedIndent = EditorGUI.indentLevel;
                            EditorGUI.indentLevel = 0;

                            Rect highlightRect = rect;
                            highlightRect.xMin -= 20f;
                            highlightRect.xMax += 6f;

                            if (e.type == EventType.Repaint && !isActive && highlightRect.Contains(e.mousePosition))
                            {
                                Color hoverColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
                                EditorGUI.DrawRect(highlightRect, hoverColor);
                            }
                            
                            rect.y += 2f;
                            rect.x += 10f;
                            rect.width -= 10f;
                            SerializedProperty element = property.GetArrayElementAtIndex(index);
                            rect.height = GetStructuredPropertyHeight(element);
                            DrawStructuredProperty(rect, element, PropertyUtils.GetContent(element.displayName));
                            
                            EditorGUI.indentLevel = cachedIndent;
                        },
                        elementHeightCallback = index =>
                        {
                            SerializedProperty element = property.GetArrayElementAtIndex(index);
                            return GetStructuredPropertyHeight(element) + 2f;
                        }
                    };

                _reorderableLists[key] = list;
            }

            Rect headerRect = EditorGUILayout.GetControlRect();
            Rect sizeRect = new Rect(headerRect.x + headerRect.width - 50f, headerRect.y, 50f, headerRect.height);
            Rect toggleRect = new Rect(headerRect.x - 4f, headerRect.y, headerRect.width - 55f + 8f, headerRect.height);
            
            EditorGUIUtility.AddCursorRect(toggleRect, MouseCursor.Arrow);
            
            HandleListDragAndDrop(property, headerRect, field);

            if (toggleRect.Contains(e.mousePosition))
            {
                if (e.type == EventType.Repaint)
                {
                    Color hoverColor = new Color(0.1f, 0.1f, 0.1f, 0.2f);
                    EditorGUI.DrawRect(toggleRect, hoverColor);
                }
            }

            EditorGUI.BeginChangeCheck();
            bool isExpanded = property.isExpanded;

            if (e.type == EventType.MouseDown && toggleRect.Contains(e.mousePosition) && e.button == 0)
            {
                property.isExpanded = !property.isExpanded;
                isExpanded = property.isExpanded;
                e.Use();
            }

            GUIContent labelContent = PropertyUtils.GetLabel(property);
            Vector2 labelSize = EditorStyles.label.CalcSize(labelContent);

            Rect labelRect = toggleRect;
            labelRect.xMin += 4f;
            Rect arrowRect = new Rect(headerRect.x + labelSize.x + 15f, headerRect.y, 20f, headerRect.height);

            EditorGUI.LabelField(labelRect, labelContent, EditorStyles.label);
            EditorGUI.Foldout(arrowRect, isExpanded, GUIContent.none, true);
            
            int newSize = EditorGUI.DelayedIntField(sizeRect, property.arraySize);

            if (EditorGUI.EndChangeCheck())
            {
                property.isExpanded = isExpanded;
                property.arraySize = newSize;
            }

            if (property.isExpanded)
                list.DoLayoutList();
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







