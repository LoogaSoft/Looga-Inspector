using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsLayerMaskAttribute))]
    public sealed class PhysicsLayerMaskDrawer : PropertyDrawerBase
    {
        private static readonly List<string> LayerNames = new();
        private static readonly List<int> LayerIndices = new();

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer || property.propertyType == SerializedPropertyType.LayerMask)
            {
                BuildLayerOptions();

                if (LayerNames.Count == 0)
                {
                    EditorGUI.LabelField(position, label.text, "No named physics layers configured");
                }
                else
                {
                    property.intValue = NamedBitMaskFieldUtility.DrawMaskField(
                        position,
                        label,
                        property.intValue,
                        LayerNames.ToArray(),
                        LayerIndices.ToArray());
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use PhysicsLayerMaskAttribute with ints or LayerMasks only");
            }

            EditorGUI.EndProperty();
        }

        private static void BuildLayerOptions()
        {
            LayerNames.Clear();
            LayerIndices.Clear();

            for (int layer = 0; layer < 32; layer++)
            {
                string layerName = LayerMask.LayerToName(layer);
                if (string.IsNullOrEmpty(layerName))
                    continue;

                LayerNames.Add(layerName);
                LayerIndices.Add(layer);
            }
        }
    }
}
