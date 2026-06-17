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
        private int _currentTabIndex;

        private readonly Dictionary<string, ReorderableList> _reorderableLists = new();
        private static readonly Dictionary<Type, InspectorLayout> _layoutCache = new();
        
        #region Built-In
        private void OnEnable()
        {
            GroupPropertiesIntoTabs();
        }
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
            
            DrawButtons(layout, true);

            DrawPropertiesScope(rootProperties, target.GetType(), "");

            DrawButtons(layout, false);
            
            serializedObject.ApplyModifiedProperties();
        }
        #endregion
        
        #region Drawers
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

                Rect boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.2f));

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
            List<InspectorElement> activeElements = elements
                .Where(element => element.tabPath.Count > level && element.tabPath[level] == currentTabName)
                .ToList();

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
                    SerializedProperty property = properties.FirstOrDefault(p => p.name == element.propertyName);
                    if (property != null)
                        DrawCustomPropertyField(property);

                    continue;
                }

                List<SerializedProperty> groupProperties = new();
                InspectorElement groupStart = element;
                string groupName = groupStart.styledGroupName;
                bool isFoldout = groupStart.styledGroupIsFoldout;

                while (i < elements.Count)
                {
                    InspectorElement groupElement = elements[i];
                    if (!groupElement.inStyledGroup
                        || groupElement.styledGroupName != groupName
                        || groupElement.styledGroupIsFoldout != isFoldout)
                    {
                        i--;
                        break;
                    }

                    SerializedProperty groupProperty = properties.FirstOrDefault(p => p.name == groupElement.propertyName);
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

        private void DrawCustomPropertyField(SerializedProperty property)
        {
            if (!PropertyUtils.IsVisible(property))
                return;
            
            DecoratorSystem.DrawDecorators(property, target);
            
            bool propertyEnabled = PropertyUtils.IsEnabled(property);
            bool isList = property.isArray && property.propertyType != SerializedPropertyType.String;
            
            //disable GUI (making the field readonly) if enabled is false
            using (new EditorGUI.DisabledScope(disabled: !propertyEnabled))
            {
                if (isList)
                    DrawReorderableList(property);
                else
                {
                    EditorGUI.BeginChangeCheck();

                    LoogaBoxAttribute boxAttribute = PropertyUtils.GetAttribute<LoogaBoxAttribute>(property);
                    LoogaFoldoutAttribute foldoutAttribute = PropertyUtils.GetAttribute<LoogaFoldoutAttribute>(property);
                    if (foldoutAttribute != null)
                    {
                        DrawFoldoutProperty(property, foldoutAttribute);
                    }
                    else if (boxAttribute != null)
                    {
                        DrawBoxProperty(property, boxAttribute);
                    }
                    else
                    {
                        bool hasCustomDrawer = CustomDrawerUtil.HasCustomDrawer(property);

                        bool customNestedFoldout = ShouldDrawNestedFoldout(property, hasCustomDrawer);
                        if (customNestedFoldout)
                        {
                            DrawNestedFoldoutProperty(property);
                        }
                        else
                        {
                            EditorGUILayout.PropertyField(property, PropertyUtils.GetLabel(property), false);

                            if (!hasCustomDrawer && property.propertyType == SerializedPropertyType.Generic &&
                                property.hasVisibleChildren && property.isExpanded)
                                DrawNestedPropertyChildren(property);
                        }
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                        PropertyUtils.CallOnFieldChangedCallbacks(property);
                }
            }
        }

        private bool ShouldDrawNestedFoldout(SerializedProperty property, bool hasCustomDrawer)
        {
            return !hasCustomDrawer
                && property.propertyType == SerializedPropertyType.Generic
                && property.hasVisibleChildren;
        }

        private void DrawNestedFoldoutProperty(SerializedProperty property)
        {
            property.isExpanded = LoogaEditorFoldouts.LoogaFoldoutSmall(
                PropertyUtils.GetLabel(property),
                property.isExpanded,
                () =>
                {
                    EditorGUI.indentLevel++;
                    DrawNestedPropertyChildren(property);
                    EditorGUI.indentLevel--;
                },
                property);
        }

        private void DrawFoldoutProperty(SerializedProperty property, LoogaFoldoutAttribute foldoutAttribute)
        {
            string title = string.IsNullOrWhiteSpace(foldoutAttribute.Title)
                ? PropertyUtils.GetLabel(property).text
                : foldoutAttribute.Title;

            string stateKey = GetFoldoutStateKey(property.serializedObject.targetObject.GetType(), property.propertyPath, title);

            if (foldoutAttribute.Style == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaFoldoutLarge(title, stateKey, foldoutAttribute.DefaultExpanded, () =>
                {
                    DrawFoldoutPropertyContent(property);
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
                new GUIContent(title),
                property.isExpanded,
                () =>
                {
                    DrawFoldoutPropertyContent(property);
                },
                property);
        }

        private void DrawFoldoutPropertyContent(SerializedProperty property)
        {
            bool hasCustomDrawer = CustomDrawerUtil.HasCustomDrawer(property);
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

            EditorGUILayout.PropertyField(property, PropertyUtils.GetLabel(property), true);
        }

        private void DrawBoxProperty(SerializedProperty property, LoogaBoxAttribute boxAttribute)
        {
            string title = string.IsNullOrWhiteSpace(boxAttribute.Title)
                ? PropertyUtils.GetLabel(property).text
                : boxAttribute.Title;

            if (boxAttribute.Style == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaBoxLarge(title, () => DrawBoxPropertyContent(property));
                return;
            }

            LoogaEditorFoldouts.LoogaBoxSmall(new GUIContent(title), () => DrawBoxPropertyContent(property));
        }

        private void DrawBoxPropertyContent(SerializedProperty property)
        {
            bool hasCustomDrawer = CustomDrawerUtil.HasCustomDrawer(property);
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

            EditorGUILayout.PropertyField(property, PropertyUtils.GetLabel(property), true);
        }

        private void DrawStyledGroup(
            InspectorElement groupStart,
            List<SerializedProperty> groupProperties,
            Type scopeType,
            string basePath)
        {
            if (!groupStart.styledGroupIsFoldout)
            {
                DrawBoxGroup(groupStart, groupProperties);
                return;
            }

            DrawFoldoutGroup(groupStart, groupProperties, scopeType, basePath);
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
                    DrawStyledGroupContent(groupProperties);
                });
                return;
            }

            bool expanded = SessionState.GetBool(stateKey, groupStart.styledGroupDefaultExpanded);
            bool newExpanded = LoogaEditorFoldouts.LoogaFoldoutSmall(new GUIContent(title), expanded, () =>
            {
                DrawStyledGroupContent(groupProperties);
            });

            if (newExpanded != expanded)
                SessionState.SetBool(stateKey, newExpanded);
        }

        private void DrawBoxGroup(
            InspectorElement groupStart,
            List<SerializedProperty> groupProperties)
        {
            string title = groupStart.styledGroupName;

            if (groupStart.styledGroupStyle == LoogaFoldoutStyle.Large)
            {
                LoogaEditorFoldouts.LoogaBoxLarge(title, () => DrawStyledGroupContent(groupProperties));
                return;
            }

            LoogaEditorFoldouts.LoogaBoxSmall(new GUIContent(title), () => DrawStyledGroupContent(groupProperties));
        }

        private void DrawStyledGroupContent(List<SerializedProperty> groupProperties)
        {
            EditorGUI.indentLevel++;
            foreach (SerializedProperty property in groupProperties)
                DrawCustomPropertyField(property);
            EditorGUI.indentLevel--;
        }

        private void DrawNestedPropertyChildren(SerializedProperty property)
        {
            var childProperties = GetNestedSerializedProperties(property);

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
                && childProperties.All(child => nestedLayout.elements.Any(element => element.propertyName == child.name));
        }

        private void DrawButtons(InspectorLayout layout, bool drawTop)
        {
            var activeButtons = layout.buttons.Where(b => b.drawAtTop == drawTop).ToList();
            
            if (activeButtons.Count == 0)
                return;
            
            EditorGUILayout.Space(2f);

            for (var i = 0; i < activeButtons.Count; i++)
            {
                var button = activeButtons[i];
                if (button.drawAtTop != drawTop)
                    continue;

                if (GUILayout.Button(button.label, GUILayout.Height(30f)))
                {
                    foreach (var t in targets)
                    {
                        button.method.Invoke(t, null);
                    }
                }

                if (i < activeButtons.Count - 1)
                    EditorGUILayout.Space(2f);
            }

            if (drawTop)
                EditorGUILayout.Space(2f);
        }

        private void DrawReorderableList(SerializedProperty property)
        {
            var field = target.GetType().GetField(property.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
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
                            rect.height = EditorGUI.GetPropertyHeight(element);
                            EditorGUI.PropertyField(rect, element, new GUIContent(element.displayName), true);
                            
                            EditorGUI.indentLevel = cachedIndent;
                        },
                        elementHeightCallback = index =>
                        {
                            SerializedProperty element = property.GetArrayElementAtIndex(index);
                            return EditorGUI.GetPropertyHeight(element) + 2f;
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

            foreach (var field in fields)
            {
                var tabAttribute = field.GetCustomAttribute<TabAttribute>();
                var tabEndAttribute = field.GetCustomAttribute<TabEndAttribute>();
                var foldoutGroupAttribute = field.GetCustomAttribute<LoogaFoldoutGroupAttribute>();
                var foldoutGroupEndAttribute = field.GetCustomAttribute<LoogaFoldoutGroupEndAttribute>();
                var boxGroupAttribute = field.GetCustomAttribute<LoogaBoxGroupAttribute>();
                var boxGroupEndAttribute = field.GetCustomAttribute<LoogaBoxGroupEndAttribute>();

                if (tabAttribute != null)
                {
                    if (!inTabGroup)
                    {
                        inTabGroup = true;
                        currentGroup = new TabGroupDefinition();
                        layout.tabGroups.Add(currentGroup);
                    }

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

                if (foldoutGroupAttribute != null)
                {
                    currentStyledGroupName = foldoutGroupAttribute.Title;
                    currentStyledGroupStyle = foldoutGroupAttribute.Style;
                    currentStyledGroupDefaultExpanded = foldoutGroupAttribute.DefaultExpanded;
                    currentStyledGroupIsFoldout = true;
                }
                else if (boxGroupAttribute != null)
                {
                    currentStyledGroupName = boxGroupAttribute.Title;
                    currentStyledGroupStyle = boxGroupAttribute.Style;
                    currentStyledGroupDefaultExpanded = true;
                    currentStyledGroupIsFoldout = false;
                }

                bool inStyledGroup = !string.IsNullOrWhiteSpace(currentStyledGroupName);
                if (currentStyledGroupIsFoldout)
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

                layout.elements.Add(currentElement);

                if (inStyledGroup && (foldoutGroupEndAttribute != null || boxGroupEndAttribute != null))
                {
                    currentStyledGroupName = null;
                    currentStyledGroupStyle = LoogaFoldoutStyle.Small;
                    currentStyledGroupDefaultExpanded = true;
                    currentStyledGroupIsFoldout = true;
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
                    drawAtTop = buttonAttribute.drawAtTop
                });
            }
            
            _layoutCache[type] = layout;
            return layout;
        }
        
        #endregion
        
        #region Helpers
        private void GroupPropertiesIntoTabs()
        {
            //get fields with these flags
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields = target.GetType().GetFields(bindingFlags);

            InspectorElement currentElement = null;
            TabGroup currentTabGroup = null;
            string currentTabName = null;
            bool inTabGroup = false;

            foreach (var field in fields)
            {
                var tabAttribute = field.GetCustomAttribute<TabAttribute>();
                var tabEndAttribute = field.GetCustomAttribute<TabEndAttribute>();

                //if tab declared
                if (tabAttribute != null)
                {
                    //if new tab
                    if (tabAttribute.tabName != currentTabName)
                    {
                        currentTabName = tabAttribute.tabName;
                        
                        //add to list of tab names inside current tab group (if we're in a tab group)
                        if (currentTabGroup != null)
                            currentTabGroup.tabNames.Add(currentTabName);
                    }
                    //if starting new tab group
                    if (!inTabGroup)
                    {
                        inTabGroup = true;
                        
                        //create new tab group and add to list of tab groups
                        List<string> tabNames = new List<string> { currentTabName };
                        currentTabGroup = new TabGroup(0, tabNames);
                    }
                    
                    //update current element
                    currentElement = new InspectorElement(field.Name, true, currentTabName);
                }
                else
                {
                    //if tab end is declared, create new element with no tab name and reset cached tab group
                    if (tabEndAttribute != null)
                    {
                        currentElement = new InspectorElement(field.Name);
                        inTabGroup = false;
                        currentTabGroup = null;
                    }
                    else
                    {
                        //if we're in a tab group, create a new element with the current tab name
                        if (currentElement != null && currentElement.inTabGroup)
                        {
                            string tabName = currentElement.tabName;
                            currentElement = new InspectorElement(field.Name, true, tabName);
                        }
                        //if we're not in a tab group, create a new element with no tab name
                        else
                            currentElement = new InspectorElement(field.Name);
                    }
                }
            }
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
            
            //filter dragged objects to only those that match the list type
            var validReferences = DragAndDrop.objectReferences.Where(obj =>
            {
                if (obj == null)
                    return false;
                
                if (obj is GameObject go && typeof(Component).IsAssignableFrom(elementType))
                    return go.GetComponent(elementType) != null;
                
                return elementType.IsInstanceOfType(obj);
            }).ToList();
            //return if none
            if (validReferences.Count == 0)
                return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                //add each item from dragged objects
                foreach (var reference in validReferences)
                {
                    Object obj = reference;
                    //extract component if game object was dragged
                    if (reference is GameObject go && typeof(Component).IsAssignableFrom(elementType))
                        obj = go.GetComponent(elementType);
                    
                    property.arraySize++;
                    property.GetArrayElementAtIndex(property.arraySize - 1).objectReferenceValue = obj;
                }
                
                //mark serialized property as changed
                GUI.changed = true;
                e.Use();
            }
        }
        #endregion
    }
}
