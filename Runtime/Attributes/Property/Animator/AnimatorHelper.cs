using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorHelper
{
    public static AnimatorController GetAnimatorController(SerializedProperty property, string controllerName)
    {
        SerializedProperty controllerProperty = FindRelativeProperty(property, controllerName);
        if (controllerProperty != null)
        {
            if (controllerProperty.objectReferenceValue is AnimatorController controller)
                return controller;
            if (controllerProperty.objectReferenceValue is Animator animator)
                return animator.runtimeAnimatorController as AnimatorController;
        }

        var targetObj = property.serializedObject.targetObject;
        var targetType = targetObj.GetType();
            
        var fieldInfo = targetType.GetField(controllerName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (fieldInfo != null)
        {
            var value = fieldInfo.GetValue(targetObj);
                
            if (value is AnimatorController controller) 
                return controller;
            if (value is Animator animator)
                return animator.runtimeAnimatorController as AnimatorController;
        }
            
        return null;
    }

    private static SerializedProperty FindRelativeProperty(SerializedProperty property, string controllerName)
    {
        if (property == null || string.IsNullOrEmpty(controllerName))
            return null;

        string path = property.propertyPath;
        int lastDot = path.LastIndexOf('.');
        if (lastDot >= 0)
        {
            string relativePath = $"{path.Substring(0, lastDot + 1)}{controllerName}";
            SerializedProperty relativeProperty = property.serializedObject.FindProperty(relativePath);
            if (relativeProperty != null)
                return relativeProperty;
        }

        return property.serializedObject.FindProperty(controllerName);
    }
}
