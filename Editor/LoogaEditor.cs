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
                DrawPropertiesWithTabs(properties, layout, basePath);
            else
            {
                foreach (var property in properties)
                    DrawCustomPropertyField(property);
            }
        }

        private void DrawPropertiesWithTabs(List<SerializedProperty> properties, InspectorLayout layout, string basePath)
        {
            int currentTabGroupIndex = 0;
            bool inTabGroup = false;

            for (int i = 0; i < layout.elements.Count; i++)
            {
                InspectorElement element = layout.elements[i];
                SerializedProperty property = properties.FirstOrDefault(p => p.name == element.propertyName);
                
                if (property == null)
                    continue;
                
                int currentTabIndex = 0;
                if (element.inTabGroup)
                {
                    string stateKey = $"{basePath}_{currentTabGroupIndex}_tab";
                    currentTabIndex = SessionState.GetInt(stateKey, 0);
                }

                if (element.inTabGroup && !inTabGroup)
                {
                    inTabGroup = true;
                    if (i > 0)
                        EditorGUILayout.Space();

                    Rect boxRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUI.DrawRect(boxRect, new Color(0f, 0f, 0f, 0.2f));
                    
                    TabGroupDefinition groupDefinition = layout.tabGroups[currentTabGroupIndex];
                    
                    int newIndex = GUILayout.Toolbar(currentTabIndex, groupDefinition.tabNames.ToArray());

                    if (newIndex != currentTabIndex)
                    {
                        string stateKey = $"{basePath}_{currentTabGroupIndex}_tab";
                        SessionState.SetInt(stateKey, newIndex);
                        currentTabIndex = newIndex;
                    }
                }
                else if (!element.inTabGroup && inTabGroup)
                {
                    inTabGroup = false;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space();
                    currentTabGroupIndex++;
                }
                
                if (!element.inTabGroup || layout.tabGroups[currentTabGroupIndex].tabNames[currentTabIndex] == element.tabName)
                    DrawCustomPropertyField(property);
            }
            
            if (inTabGroup)
                EditorGUILayout.EndVertical();
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
                    
                    bool hasCustomDrawer = CustomDrawerUtil.HasCustomDrawer(property);
                    
                    EditorGUILayout.PropertyField(property, PropertyUtils.GetLabel(property), false);

                    if (!hasCustomDrawer && property.propertyType == SerializedPropertyType.Generic &&
                        property.hasVisibleChildren && property.isExpanded)
                    {
                        var childProperties = GetNestedSerializedProperties(property);
                        Type nestedType = CustomDrawerUtil.GetTargetType(property);
                        
                        if (nestedType != null)
                            DrawPropertiesScope(childProperties, nestedType, property.propertyPath);
                        else
                        {
                            foreach (var childProperty in childProperties)
                                DrawCustomPropertyField(childProperty);
                        }
                    }
                    
                    if (EditorGUI.EndChangeCheck())
                        PropertyUtils.CallOnFieldChangedCallbacks(property);
                }
            }
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
            
            InspectorElement currentElement = null;
            TabGroupDefinition currentGroup = null;
            string currentTabName = null;
            bool inTabGroup = false;

            foreach (var field in fields)
            {
                var tabAttribute = field.GetCustomAttribute<TabAttribute>();
                var tabEndAttribute = field.GetCustomAttribute<TabEndAttribute>();

                if (tabAttribute != null)
                {
                    if (tabAttribute.tabName != currentTabName)
                    {
                        currentTabName = tabAttribute.tabName;
                        
                        if (currentGroup != null)
                            currentGroup.tabNames.Add(currentTabName);
                    }

                    if (!inTabGroup)
                    {
                        inTabGroup = true;
                        currentGroup = new TabGroupDefinition();
                        currentGroup.tabNames.Add(currentTabName);
                        layout.tabGroups.Add(currentGroup);
                    }
                    
                    currentElement = new InspectorElement(field.Name, true, currentTabName);
                }
                else
                {
                    if (tabEndAttribute != null)
                    {
                        currentElement = new InspectorElement(field.Name);
                        inTabGroup = false;
                        currentGroup = null;
                    }
                    else
                    {
                        if (currentElement != null && currentElement.inTabGroup)
                            currentElement = new InspectorElement(field.Name, true, currentTabName);
                        else
                            currentElement = new InspectorElement(field.Name);
                    }
                }
                
                layout.elements.Add(currentElement);
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