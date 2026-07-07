using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(NavMeshAreaMaskAttribute))]
    public sealed class NavMeshAreaMaskDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                string[] areaNames = GameObjectUtility.GetNavMeshAreaNames();

                if (areaNames.Length == 0)
                {
                    EditorGUI.LabelField(position, label.text, "No NavMesh areas configured");
                }
                else
                {
                    property.intValue = NamedBitMaskFieldUtility.DrawMaskField(
                        position,
                        label,
                        property.intValue,
                        areaNames,
                        GetAreaIndices(areaNames));
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use NavMeshAreaMaskAttribute with ints only");
            }

            EditorGUI.EndProperty();
        }

        private static int[] GetAreaIndices(string[] areaNames)
        {
            int[] areaIndices = new int[areaNames.Length];

            for (int i = 0; i < areaNames.Length; i++)
                areaIndices[i] = GameObjectUtility.GetNavMeshAreaFromName(areaNames[i]);

            return areaIndices;
        }
    }
}
