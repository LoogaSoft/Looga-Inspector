using System.Reflection;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaMemberValueUtility
    {
        public static object GetValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
                return null;

            FieldInfo field = ReflectionUtils.GetField(target, memberName);
            if (field != null)
                return field.GetValue(target);

            PropertyInfo property = ReflectionUtils.GetProperty(target, memberName);
            if (property != null)
                return property.GetValue(target, null);

            MethodInfo method = ReflectionUtils.GetMethod(target, memberName);
            if (method != null && method.GetParameters().Length == 0)
                return method.Invoke(target, null);

            return null;
        }
    }
}
