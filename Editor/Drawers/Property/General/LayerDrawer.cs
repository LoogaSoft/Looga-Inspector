using LoogaSoft.Inspector.Editor;
using UnityEditor;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;

[CustomPropertyDrawer(typeof(LayerAttribute))]
public class LayerDrawer : PropertyDrawerBase
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
            int currentLayerIndex = LayerMask.NameToLayer(property.stringValue);
            
            if (currentLayerIndex < 0) 
                currentLayerIndex = 0;
            
            int newLayerIndex = EditorGUI.LayerField(position, label, currentLayerIndex);

            property.stringValue = LayerMask.LayerToName(newLayerIndex);
        }
        else
        {
            EditorGUI.LabelField(position, label.text, "Use LayerAttribute with ints or strings only");
        }
        
        EditorGUI.EndProperty();
    }
}
