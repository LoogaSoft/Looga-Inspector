using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(AssetLinkAttribute))]
    public sealed class AssetLinkDrawer : PropertyDrawerBase
    {
        private const float ButtonWidth = 42f;
        private const float Gap = 3f;

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            AssetLinkAttribute linkAttribute = (AssetLinkAttribute)attribute;
            if (property.propertyType != SerializedPropertyType.ObjectReference)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float buttonAreaWidth = property.objectReferenceValue == null
                ? 0f
                : (linkAttribute.ShowPingButton ? ButtonWidth * 2f + Gap * 2f : ButtonWidth + Gap);

            Rect fieldRect = position;
            fieldRect.width -= buttonAreaWidth;

            using (new EditorGUI.DisabledScope(linkAttribute.ReadOnly))
                EditorGUI.PropertyField(fieldRect, property, label, true);

            Object asset = property.objectReferenceValue;
            if (asset == null)
                return;

            Rect openRect = new(fieldRect.xMax + Gap, position.y, ButtonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(openRect, "Open"))
                AssetDatabase.OpenAsset(asset);

            if (!linkAttribute.ShowPingButton)
                return;

            Rect pingRect = new(openRect.xMax + Gap, position.y, ButtonWidth, EditorGUIUtility.singleLineHeight);
            if (GUI.Button(pingRect, "Ping"))
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }
    }
}
