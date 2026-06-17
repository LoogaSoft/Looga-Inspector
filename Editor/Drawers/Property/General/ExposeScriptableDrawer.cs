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

        private UnityEditor.Editor _editor;
        
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            
            bool objectValid = property.objectReferenceValue != null;
            bool canCreateAsset = !objectValid && TryGetScriptableObjectType(out Type scriptableObjectType);

            float indentOffset = EditorGUI.indentLevel * 15f;
            float labelWidth = EditorGUIUtility.labelWidth - indentOffset;

            Rect labelRect = new Rect(position.x, position.y, labelWidth, LineHeight);
            if (objectValid)
                property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);
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

            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);

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
            ScriptableObject asset = ScriptableObject.CreateInstance(scriptableObjectType);
            string assetPath = AssetDatabase.GenerateUniqueAssetPath(GetDefaultAssetPath(property, scriptableObjectType));

            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            property.objectReferenceValue = asset;
            property.serializedObject.ApplyModifiedProperties();

            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        private static string GetDefaultAssetPath(SerializedProperty property, Type scriptableObjectType)
        {
            UnityEngine.Object targetObject = property.serializedObject.targetObject;
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

            string assetName = $"{ObjectNames.NicifyVariableName(scriptableObjectType.Name)}.asset";
            return $"{directory.Replace('\\', '/')}/{assetName}";
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            return LineHeight;
        }
    }
}
