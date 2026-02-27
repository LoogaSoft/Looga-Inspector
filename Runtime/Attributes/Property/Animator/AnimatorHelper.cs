using System.Reflection;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class AnimatorHelper
{
    public static AnimatorController GetAnimatorController(SerializedProperty property, string controllerName)
    {
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
}
