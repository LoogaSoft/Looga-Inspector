using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(NavMeshAreaAttribute))]
    public sealed class NavMeshAreaDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            string[] areaNames = GameObjectUtility.GetNavMeshAreaNames();
            if (areaNames.Length == 0)
            {
                EditorGUI.LabelField(position, label.text, "No NavMesh areas configured");
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                int currentIndex = FindAreaNameIndex(areaNames, property.intValue);
                int nextIndex = LoogaGUI.Popup(position, label.text, currentIndex, areaNames);
                property.intValue = GameObjectUtility.GetNavMeshAreaFromName(areaNames[nextIndex]);
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                int currentIndex = Mathf.Max(0, System.Array.IndexOf(areaNames, property.stringValue));
                int nextIndex = LoogaGUI.Popup(position, label.text, currentIndex, areaNames);
                property.stringValue = areaNames[nextIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use NavMeshAreaAttribute with ints or strings only");
            }

            EditorGUI.EndProperty();
        }

        private static int FindAreaNameIndex(string[] areaNames, int areaIndex)
        {
            for (int i = 0; i < areaNames.Length; i++)
            {
                if (GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]) == areaIndex)
                    return i;
            }

            return 0;
        }
    }
}
