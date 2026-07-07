using System;
using System.Collections.Generic;
using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(HierarchicalAssetDropdownAttribute))]
    public sealed class HierarchicalAssetDropdownDrawer : PropertyDrawerBase
    {
        private const float PingButtonWidth = 24f;
        private const float Gap = 3f;

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            HierarchicalAssetDropdownAttribute dropdown = (HierarchicalAssetDropdownAttribute)attribute;
            Type assetType = GetAssetType(property, dropdown);
            if (assetType == null)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            Rect fieldRect = position;
            fieldRect.width -= property.propertyType == SerializedPropertyType.ObjectReference ? PingButtonWidth + Gap : 0f;

            Rect buttonRect = EditorGUI.PrefixLabel(fieldRect, label);
            string currentLabel = GetCurrentLabel(property, dropdown);
            if (GUI.Button(buttonRect, currentLabel, EditorStyles.popup))
                ShowMenu(buttonRect, property, dropdown, assetType);

            if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null)
                return;

            Rect pingRect = new(fieldRect.xMax + Gap, position.y, PingButtonWidth, position.height);
            if (GUI.Button(pingRect, EditorGUIUtility.IconContent("d_ViewToolZoom")))
                EditorGUIUtility.PingObject(property.objectReferenceValue);
        }

        private static Type GetAssetType(SerializedProperty property, HierarchicalAssetDropdownAttribute dropdown)
        {
            if (dropdown.AssetType != null && typeof(Object).IsAssignableFrom(dropdown.AssetType))
                return dropdown.AssetType;

            if (property.propertyType == SerializedPropertyType.String)
                return null;

            if (property.propertyType != SerializedPropertyType.ObjectReference)
                return null;

            Type type = CustomDrawerUtil.GetTargetType(property);
            return type != null && typeof(Object).IsAssignableFrom(type) ? type : typeof(Object);
        }

        private static string GetCurrentLabel(SerializedProperty property, HierarchicalAssetDropdownAttribute dropdown)
        {
            if (property.propertyType == SerializedPropertyType.String)
                return string.IsNullOrWhiteSpace(property.stringValue) ? "None" : property.stringValue;

            Object asset = property.objectReferenceValue;
            return asset == null ? "None" : GetAssetPathLabel(asset, dropdown);
        }

        private static void ShowMenu(
            Rect buttonRect,
            SerializedProperty property,
            HierarchicalAssetDropdownAttribute dropdown,
            Type assetType)
        {
            GenericMenu menu = new();
            if (dropdown.IncludeNone)
                menu.AddItem(new GUIContent("None"), IsNoneSelected(property), () => SetValue(property, null, null, dropdown));

            List<Object> assets = FindAssets(assetType, dropdown.SearchFilter);
            for (int i = 0; i < assets.Count; i++)
            {
                Object asset = assets[i];
                string label = GetAssetPathLabel(asset, dropdown).Replace('.', '/');
                bool selected = property.propertyType == SerializedPropertyType.ObjectReference
                    ? property.objectReferenceValue == asset
                    : property.stringValue == GetAssetPathLabel(asset, dropdown);

                menu.AddItem(new GUIContent(label), selected, () => SetValue(property, asset, GetAssetPathLabel(asset, dropdown), dropdown));
            }

            menu.DropDown(buttonRect);
        }

        private static List<Object> FindAssets(Type assetType, string searchFilter)
        {
            string filter = $"t:{assetType.Name}";
            if (!string.IsNullOrWhiteSpace(searchFilter))
                filter += $" {searchFilter}";

            string[] guids = AssetDatabase.FindAssets(filter);
            List<Object> assets = new(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Object asset = AssetDatabase.LoadAssetAtPath(path, assetType);
                if (asset != null)
                    assets.Add(asset);
            }

            assets.Sort((left, right) => string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase));
            return assets;
        }

        private static void SetValue(
            SerializedProperty property,
            Object asset,
            string pathLabel,
            HierarchicalAssetDropdownAttribute dropdown)
        {
            property.serializedObject.Update();
            if (property.propertyType == SerializedPropertyType.ObjectReference)
                property.objectReferenceValue = asset;
            else if (property.propertyType == SerializedPropertyType.String)
                property.stringValue = pathLabel ?? string.Empty;

            property.serializedObject.ApplyModifiedProperties();
        }

        private static bool IsNoneSelected(SerializedProperty property)
        {
            return property.propertyType == SerializedPropertyType.ObjectReference
                ? property.objectReferenceValue == null
                : string.IsNullOrWhiteSpace(property.stringValue);
        }

        private static string GetAssetPathLabel(Object asset, HierarchicalAssetDropdownAttribute dropdown)
        {
            if (asset == null)
                return "None";

            object value = GetMemberValue(asset, dropdown.PathMemberName);
            return string.IsNullOrWhiteSpace(value?.ToString()) ? asset.name : value.ToString();
        }

        private static object GetMemberValue(Object asset, string memberName)
        {
            if (asset == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Type type = asset.GetType();
            FieldInfo field = type.GetField(memberName, flags);
            if (field != null)
                return field.GetValue(asset);

            PropertyInfo property = type.GetProperty(memberName, flags);
            return property?.GetValue(asset);
        }
    }
}
