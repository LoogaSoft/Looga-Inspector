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
        private const float CreateButtonGap = 4f;
        private const float FoldoutArrowInset = 4f;
        private const float FoldoutArrowSize = 8f;
        private const float FoldoutLabelGap = 4f;

        private UnityEditor.Editor _editor;
        
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            bool objectValid = property.objectReferenceValue != null;
            TryGetScriptableObjectType(out Type scriptableObjectType);
            bool canCreateAsset = !objectValid && scriptableObjectType != null;

            float indentOffset = EditorGUI.indentLevel * 15f;
            float labelWidth = Mathf.Clamp(
                EditorGUIUtility.labelWidth - indentOffset,
                80f,
                Mathf.Max(80f, position.width - CreateButtonWidth - CreateButtonGap - 80f));

            Rect labelRect = new Rect(position.x, position.y, labelWidth, LineHeight);
            if (objectValid)
                property.isExpanded = DrawInlineFoldout(labelRect, label, property.isExpanded);
            else
            {
                property.isExpanded = false;
                EditorGUI.LabelField(labelRect, label);
            }
            
            Rect fieldRect = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, LineHeight);
            Rect createButtonRect = default;
            if (canCreateAsset)
            {
                createButtonRect = new Rect(
                    fieldRect.xMax - CreateButtonWidth,
                    fieldRect.y,
                    CreateButtonWidth,
                    LineHeight);

                fieldRect.width -= CreateButtonWidth + CreateButtonGap;
            }

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
            
            if (property.isExpanded && objectValid)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUI.indentLevel++;
                
                UnityEditor.Editor.CreateCachedEditor(property.objectReferenceValue, null, ref _editor);
                
                using (LoogaEditorFoldouts.ContainedFoldoutScope())
                {
                    _editor?.OnInspectorGUI();
                }
                
                EditorGUI.indentLevel--;
                EditorGUILayout.EndVertical();
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

        private static bool DrawInlineFoldout(Rect position, GUIContent label, bool expanded)
        {
            Event current = Event.current;
            bool newExpanded = expanded;

            Rect arrowRect = new(
                position.x + FoldoutArrowInset,
                position.y + (position.height - FoldoutArrowSize) * 0.5f,
                FoldoutArrowSize,
                FoldoutArrowSize);

            Rect labelRect = new(
                arrowRect.xMax + FoldoutLabelGap,
                position.y,
                Mathf.Max(0f, position.xMax - arrowRect.xMax - FoldoutLabelGap),
                position.height);

            if (current.type == EventType.MouseDown
                && current.button == 0
                && position.Contains(current.mousePosition))
            {
                newExpanded = !expanded;
                current.Use();
            }

            if (current.type == EventType.Repaint)
                DrawFoldoutArrow(arrowRect, expanded);

            EditorGUI.LabelField(labelRect, label);
            return newExpanded;
        }

        private static void DrawFoldoutArrow(Rect arrowRect, bool expanded)
        {
            Color previousColor = Handles.color;
            Handles.color = EditorGUIUtility.isProSkin
                ? new Color(0.68f, 0.68f, 0.68f, 1f)
                : new Color(0.28f, 0.28f, 0.28f, 1f);

            Vector2 center = arrowRect.center;
            float radius = FoldoutArrowSize * 0.5f;
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
            return LineHeight;
        }
    }
}
