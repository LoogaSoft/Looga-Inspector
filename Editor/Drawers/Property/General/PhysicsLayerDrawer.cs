using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(PhysicsLayerAttribute))]
    public sealed class PhysicsLayerDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = EditorGUI.LayerField(position, label, property.intValue);
            }
            else if (property.propertyType == SerializedPropertyType.String)
            {
                int currentLayer = LayerMask.NameToLayer(property.stringValue);
                if (currentLayer < 0)
                    currentLayer = 0;

                int nextLayer = EditorGUI.LayerField(position, label, currentLayer);
                property.stringValue = LayerMask.LayerToName(nextLayer);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use PhysicsLayerAttribute with ints or strings only");
            }

            EditorGUI.EndProperty();
        }
    }
}
