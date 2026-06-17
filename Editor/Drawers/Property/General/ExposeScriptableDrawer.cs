using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(ExposeScriptableAttribute))]
    public class ExposeScriptableDrawer : PropertyDrawerBase
    {
        private static readonly float LineHeight = EditorGUIUtility.singleLineHeight;
        private const float CreateButtonWidth = 58f;
        private const float HeaderHeight = 22f;
        private const float HeaderLeftInset = 6f;
        private const float HeaderFieldGap = 6f;
        private const float HeaderArrowSize = 9f;
        private const float HeaderArrowRightInset = 10f;
        private const float HeaderArrowLeftNudge = 5f;
        
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            bool objectValid = property.objectReferenceValue != null;
            TryGetScriptableObjectType(out Type scriptableObjectType);
            bool canCreateAsset = !objectValid && scriptableObjectType != null;

            float spacing = EditorGUIUtility.standardVerticalSpacing;
            Rect boxRect = new(
                position.x,
                position.y,
                position.width,
                position.height);
            Rect headerRect = new(
                boxRect.x,
                boxRect.y + 1f,
                boxRect.width,
                LineHeight + 2f);
            Rect arrowRect = objectValid
                ? GetHeaderArrowRect(headerRect)
                : default;
            Rect createButtonRect = canCreateAsset
                ? new Rect(
                    boxRect.xMax - CreateButtonWidth,
                    boxRect.y,
                    CreateButtonWidth,
                    boxRect.height)
                : default;
            Rect contentRect = new(
                headerRect.x + HeaderLeftInset,
                headerRect.y + (headerRect.height - LineHeight) * 0.5f,
                headerRect.width - HeaderLeftInset,
                LineHeight);
            Rect rightLimitRect = objectValid
                ? arrowRect
                : canCreateAsset
                    ? createButtonRect
                    : new Rect(headerRect.xMax, headerRect.y, 0f, headerRect.height);
            float labelWidth = Mathf.Clamp(EditorGUIUtility.labelWidth * 0.65f, 90f, contentRect.width * 0.5f);
            Rect labelRect = new(contentRect.x, contentRect.y, labelWidth, LineHeight);
            Rect fieldRect = new(
                labelRect.xMax + HeaderFieldGap,
                contentRect.y,
                Mathf.Max(0f, rightLimitRect.x - labelRect.xMax - GetFieldRightGap(canCreateAsset)),
                LineHeight);

            DrawFoldoutBackground(boxRect, headerRect);
            EditorGUI.LabelField(labelRect, label);

            Type objectFieldType = scriptableObjectType ?? typeof(ScriptableObject);
            UnityEngine.Object newValue = EditorGUI.ObjectField(
                fieldRect,
                property.objectReferenceValue,
                objectFieldType,
                false);

            if (newValue != property.objectReferenceValue)
                property.objectReferenceValue = newValue;

            if (canCreateAsset && GUI.Button(createButtonRect, "Create"))
                ShowCreateMenu(property, scriptableObjectType);

            if (objectValid)
                property.isExpanded = DrawHeaderFoldout(headerRect, fieldRect, arrowRect, property.isExpanded);
            
            if (property.isExpanded && objectValid)
            {
                Rect inlineContentRect = new(
                    boxRect.x + LoogaEditorFoldouts.SmallPaddingX,
                    headerRect.yMax + spacing,
                    boxRect.width - LoogaEditorFoldouts.SmallPaddingX * 2f,
                    Mathf.Max(0f, boxRect.yMax - headerRect.yMax - spacing - LoogaEditorFoldouts.SmallPaddingY));

                DrawInlineScriptableObject(inlineContentRect, property.objectReferenceValue);
            }

            EditorGUI.EndProperty();
        }

        private bool TryGetScriptableObjectType(out Type scriptableObjectType)
        {
            if (fieldInfo == null)
            {
                scriptableObjectType = null;
                return false;
            }

            scriptableObjectType = fieldInfo.FieldType;

            if (scriptableObjectType.IsArray)
                scriptableObjectType = scriptableObjectType.GetElementType();
            else if (scriptableObjectType.IsGenericType && scriptableObjectType.GetGenericArguments().Length == 1)
                scriptableObjectType = scriptableObjectType.GetGenericArguments()[0];

            return scriptableObjectType != null
                && typeof(ScriptableObject).IsAssignableFrom(scriptableObjectType);
        }

        private static void DrawFoldoutBackground(Rect boxRect, Rect headerRect)
        {
            GUI.Box(boxRect, GUIContent.none, LoogaEditorFoldouts.SmallBoxStyle);

            Event current = Event.current;
            if (headerRect.Contains(current.mousePosition))
                EditorGUI.DrawRect(headerRect, new Color(1f, 1f, 1f, 0.05f));
        }

        private static bool DrawHeaderFoldout(Rect headerRect, Rect fieldRect, Rect arrowRect, bool expanded)
        {
            Event current = Event.current;
            bool newExpanded = expanded;

            bool canToggle = headerRect.Contains(current.mousePosition)
                && !fieldRect.Contains(current.mousePosition);

            if (current.type == EventType.MouseDown && current.button == 0 && canToggle)
            {
                newExpanded = !expanded;
                current.Use();
            }

            if (current.type == EventType.Repaint)
                DrawFoldoutArrow(arrowRect, expanded);

            return newExpanded;
        }

        private static Rect GetHeaderArrowRect(Rect headerRect)
        {
            return new Rect(
                headerRect.xMax - HeaderArrowRightInset - HeaderArrowLeftNudge - HeaderArrowSize,
                headerRect.y + (headerRect.height - HeaderArrowSize) * 0.5f,
                HeaderArrowSize,
                HeaderArrowSize);
        }

        private static float GetFieldRightGap(bool hasCreateButton)
        {
            return hasCreateButton
                ? HeaderFieldGap
                : HeaderFieldGap * 2f;
        }

        private static void DrawInlineScriptableObject(Rect position, UnityEngine.Object scriptableObject)
        {
            if (scriptableObject == null)
                return;

            SerializedObject serializedObject = new(scriptableObject);
            serializedObject.Update();

            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            float y = position.y;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel++;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                float propertyHeight = EditorGUI.GetPropertyHeight(iterator, includeChildren: true);
                Rect propertyRect = new(position.x, y, position.width, propertyHeight);

                using (new EditorGUI.DisabledScope(iterator.propertyPath == "m_Script"))
                    EditorGUI.PropertyField(propertyRect, iterator, includeChildren: true);

                y += propertyHeight + spacing;
            }

            EditorGUI.indentLevel = oldIndent;
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawFoldoutArrow(Rect arrowRect, bool expanded)
        {
            Color previousColor = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.68f, 0.68f, 0.68f, 1f)
                : new Color(0.28f, 0.28f, 0.28f, 1f);

            Vector2 center = arrowRect.center;
            float radius = HeaderArrowSize * 0.5f;
            float verticalRadius = radius * Mathf.Sqrt(3f) * 0.5f;
            Vector3[] points = expanded
                ? new[]
                {
                    new Vector3(center.x - radius, center.y - verticalRadius * 0.5f, 0f),
                    new Vector3(center.x + radius, center.y - verticalRadius * 0.5f, 0f),
                    new Vector3(center.x, center.y + verticalRadius, 0f)
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

        private static void ShowCreateMenu(SerializedProperty property, Type scriptableObjectType)
        {
            List<Type> concreteTypes = GetConcreteScriptableObjectTypes(scriptableObjectType);
            if (concreteTypes.Count == 0)
                return;

            if (concreteTypes.Count == 1)
            {
                CreateAndAssignAsset(property, concreteTypes[0]);
                return;
            }

            GenericMenu menu = new();
            foreach (Type concreteType in concreteTypes)
            {
                Type capturedType = concreteType;
                menu.AddItem(
                    new GUIContent(ObjectNames.NicifyVariableName(concreteType.Name)),
                    false,
                    () => CreateAndAssignAsset(property, capturedType));
            }

            menu.ShowAsContext();
        }

        private static List<Type> GetConcreteScriptableObjectTypes(Type scriptableObjectType)
        {
            List<Type> concreteTypes = new();

            if (!scriptableObjectType.IsAbstract && !scriptableObjectType.IsInterface)
                concreteTypes.Add(scriptableObjectType);

            concreteTypes.AddRange(TypeCache.GetTypesDerivedFrom(scriptableObjectType)
                .Where(type => typeof(ScriptableObject).IsAssignableFrom(type)
                    && !type.IsAbstract
                    && !type.IsGenericType));

            return concreteTypes
                .Distinct()
                .OrderBy(type => type.Name)
                .ToList();
        }

        private static void CreateAndAssignAsset(SerializedProperty property, Type scriptableObjectType)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
            string propertyPath = property.propertyPath;

            EditorApplication.delayCall += () => CreateAndAssignAsset(targetObject, propertyPath, scriptableObjectType);
        }

        private static void CreateAndAssignAsset(UnityEngine.Object targetObject, string propertyPath, Type scriptableObjectType)
        {
            if (targetObject == null || string.IsNullOrWhiteSpace(propertyPath))
                return;

            ScriptableObject asset = ScriptableObject.CreateInstance(scriptableObjectType);
            string assetPath = EditorUtility.SaveFilePanelInProject(
                "Create Scriptable Object",
                GetDefaultAssetName(scriptableObjectType),
                "asset",
                "Choose where to save the new asset.",
                GetDefaultDirectory(targetObject));

            if (string.IsNullOrWhiteSpace(assetPath))
            {
                UnityEngine.Object.DestroyImmediate(asset);
                return;
            }

            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SerializedObject serializedObject = new(targetObject);
            SerializedProperty property = serializedObject.FindProperty(propertyPath);
            if (property == null)
            {
                EditorGUIUtility.PingObject(asset);
                return;
            }

            property.objectReferenceValue = asset;
            serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(asset);
        }

        private static string GetDefaultDirectory(UnityEngine.Object targetObject)
        {
            string targetPath = AssetDatabase.GetAssetPath(targetObject);
            string directory = "Assets";

            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                directory = Directory.Exists(targetPath)
                    ? targetPath
                    : Path.GetDirectoryName(targetPath);
            }

            if (string.IsNullOrWhiteSpace(directory))
                directory = "Assets";

            return directory.Replace('\\', '/');
        }

        private static string GetDefaultAssetName(Type scriptableObjectType)
        {
            return ObjectNames.NicifyVariableName(scriptableObjectType.Name);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            float height = HeaderHeight;

            if (property.isExpanded && property.objectReferenceValue != null)
                height += GetInlineScriptableObjectHeight(property.objectReferenceValue)
                    + LoogaEditorFoldouts.SmallPaddingY;

            return height;
        }

        private static float GetInlineScriptableObjectHeight(UnityEngine.Object scriptableObject)
        {
            if (scriptableObject == null)
                return 0f;

            SerializedObject serializedObject = new(scriptableObject);
            SerializedProperty iterator = serializedObject.GetIterator();
            bool enterChildren = true;
            float height = EditorGUIUtility.standardVerticalSpacing;

            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                height += EditorGUI.GetPropertyHeight(iterator, includeChildren: true)
                    + EditorGUIUtility.standardVerticalSpacing;
            }

            return height;
        }
    }
}
