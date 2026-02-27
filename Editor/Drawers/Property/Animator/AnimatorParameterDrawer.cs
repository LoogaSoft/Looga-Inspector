using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
#if ZLINQ_SUPPORT
using ZLinq;
#else
using System.Linq;
#endif

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorParameterAttribute))]
    public class AnimatorParameterDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            var apAttribute = (AnimatorParameterAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.Integer &&
                property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "AnimatorParameterAttribute can only be used with ints or strings");
                EditorGUI.EndProperty();
                return;
            }
            
            var controller = AnimatorHelper.GetAnimatorController(property, apAttribute.animatorControllerName);

            if (controller == null)
            {
                EditorGUI.LabelField(position, label.text, "Animator Controller not found");
                return;
            }
            
            List<string> paramNames = controller.parameters
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(p => p.name)
                .ToList();
            
            int[] paramHashes = controller.parameters
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(p => p.nameHash)
                .ToArray();
            
            paramNames.Insert(0, "None");
            int[] paramHashesWithNone = new int[paramHashes.Length + 1];
            paramHashesWithNone[0] = 0;
            paramHashes.CopyTo(paramHashesWithNone, 1);

            var currentIndex = -1;
            
            if (property.propertyType == SerializedPropertyType.Integer)
                currentIndex = Array.IndexOf(paramHashesWithNone, property.intValue);
            else if (property.propertyType == SerializedPropertyType.String)
                currentIndex = paramNames.IndexOf(property.stringValue);
            
            if (currentIndex < 0) currentIndex = 0;
            
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, paramNames.ToArray());

            if (newIndex >= 0 && newIndex != currentIndex)
            {
                if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = paramHashesWithNone[newIndex];
                else if (property.propertyType == SerializedPropertyType.String)
                    property.stringValue = paramNames[newIndex];
            }
            
            EditorGUI.EndProperty();
        }
    }
}