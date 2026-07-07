using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaShaderEditorUtility
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public static Shader ResolveShader(SerializedProperty property, string memberName)
        {
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            object memberValue = GetMemberValue(target, memberName);

            return memberValue switch
            {
                Material material => material != null ? material.shader : null,
                Shader shader => shader,
                _ => null
            };
        }

        private static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            System.Type type = target.GetType();

            FieldInfo field = type.GetField(memberName, MemberFlags);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = type.GetProperty(memberName, MemberFlags);
            if (property != null)
                return property.GetValue(target, null);

            MethodInfo method = type.GetMethod(memberName, MemberFlags);
            if (method != null && method.GetParameters().Length == 0)
                return method.Invoke(target, null);

            return null;
        }
    }
}
