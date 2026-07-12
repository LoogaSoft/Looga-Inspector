using System;
using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

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
                EditorGUI.EndProperty();
                return;
            }
            
            bool filterByParameterType = apAttribute.filterByParameterType || IsTriggerProperty(property);
            AnimatorControllerParameterType parameterType = apAttribute.filterByParameterType
                ? apAttribute.parameterType
                : AnimatorControllerParameterType.Trigger;

            AnimatorControllerParameter[] parameterList = LoogaInspectorQueryUtility.FilterAnimatorParameters(
                controller.parameters,
                filterByParameterType,
                parameterType);

            List<string> paramNames = new(parameterList.Length + 1) { "None" };
            int[] paramHashesWithNone = new int[parameterList.Length + 1];

            for (int i = 0; i < parameterList.Length; i++)
            {
                AnimatorControllerParameter parameter = parameterList[i];
                paramNames.Add(parameter.name);
                paramHashesWithNone[i + 1] = parameter.nameHash;
            }

            var currentIndex = -1;
            
            if (property.propertyType == SerializedPropertyType.Integer)
                currentIndex = Array.IndexOf(paramHashesWithNone, property.intValue);
            else if (property.propertyType == SerializedPropertyType.String)
                currentIndex = paramNames.IndexOf(property.stringValue);
            
            if (currentIndex < 0) currentIndex = 0;
            
            var newIndex = LoogaGUI.Popup(position, label.text, currentIndex, paramNames.ToArray());

            if (newIndex >= 0 && newIndex != currentIndex)
            {
                if (property.propertyType == SerializedPropertyType.Integer)
                    property.intValue = paramHashesWithNone[newIndex];
                else if (property.propertyType == SerializedPropertyType.String)
                    property.stringValue = paramNames[newIndex];
            }
            
            EditorGUI.EndProperty();
        }

        private static bool IsTriggerProperty(SerializedProperty property)
        {
            return property != null
                && property.propertyType == SerializedPropertyType.String
                && property.name.IndexOf("trigger", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
