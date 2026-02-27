using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
#if ZLINQ_SUPPORT
using ZLinq;
#else
using System.Linq;
#endif

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorLayerAttribute))]
    public class AnimatorLayerDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            var alAttribute = (AnimatorLayerAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.Integer &&
                property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "AnimatorLayerAttribute can only be used with ints or strings");
                EditorGUI.EndProperty();
                return;
            }
            
            var controller = AnimatorHelper.GetAnimatorController(property, alAttribute.animatorControllerName);
            
            if (controller == null)
            {
                EditorGUI.LabelField(position, label.text, "Animator Controller not found");
                return;
            }
            
            List<string> layerNames = controller.layers
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(l => l.name)
                .ToList();
            layerNames.Insert(0, "None");

            int currentIndex = -1;

            if (property.propertyType == SerializedPropertyType.Integer)
                currentIndex = property.intValue + 1;
            else if (property.propertyType == SerializedPropertyType.String)
            {
                string currentLayerName = property.stringValue;
                currentIndex = string.IsNullOrEmpty(currentLayerName) ? 0 : layerNames.IndexOf(currentLayerName);
            }
            
            if (currentIndex < 0 || currentIndex >= layerNames.Count) 
                currentIndex = 0;
            
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, layerNames.ToArray());
            
            if (newIndex != currentIndex) 
            {
                if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = newIndex - 1;
                else if (property.propertyType == SerializedPropertyType.String)
                    property.stringValue = newIndex == 0 ? "" : layerNames[newIndex];
            }
            
            EditorGUI.EndProperty();
        }
    }
}