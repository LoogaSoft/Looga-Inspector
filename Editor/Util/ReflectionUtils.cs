using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using System.Linq;


namespace LoogaSoft.Inspector.Editor
{
    public static class ReflectionUtils
    {
        public static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("Target cannot be null!");
                yield break;
            }
            
            List<Type> types = GetAllTypes(target);

            for (int i = types.Count - 1; i >= 0; i--)
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance 
                                     | BindingFlags.DeclaredOnly | BindingFlags.Static;
                
                IEnumerable<FieldInfo> fields = types[i].GetFields(flags)
                    .Where(predicate);
                
                foreach (FieldInfo field in fields)
                    yield return field;
            }
        }
        public static IEnumerable<PropertyInfo> GetAllProperties(object target, Func<PropertyInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("Target cannot be null!");
                yield break;
            }
            
            List<Type> types = GetAllTypes(target);
            
            for (int i = types.Count - 1; i >= 0; i--)
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance 
                                     | BindingFlags.DeclaredOnly | BindingFlags.Static;
                
                IEnumerable<PropertyInfo> properties = types[i].GetProperties(flags)
                    .Where(predicate);
                
                foreach (PropertyInfo property in properties)
                    yield return property;
            }
        }

        public static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("Target cannot be null!");
                yield break;
            }
            
            List<Type> types = GetAllTypes(target);
            
            for (int i = types.Count - 1; i >= 0; i--)
            {
                BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance 
                                     | BindingFlags.DeclaredOnly | BindingFlags.Static;
                
                IEnumerable<MethodInfo> methods = types[i].GetMethods(flags)
                    .Where(predicate);
                
                foreach (MethodInfo method in methods)
                    yield return method;           
            }
        }
        
        public static FieldInfo GetField(object target, string fieldName) => GetAllFields(target, f => f.Name == fieldName).FirstOrDefault();
        public static PropertyInfo GetProperty(object target, string propertyName) => GetAllProperties(target, p => p.Name == propertyName).FirstOrDefault();
        public static MethodInfo GetMethod(object target, string methodName) => GetAllMethods(target, m => m.Name == methodName).FirstOrDefault();

        private static List<Type> GetAllTypes(object target)
        {
            List<Type> types = new List<Type>()
            {
                target.GetType()
            };
            
            while (types[^1].BaseType != null)
                types.Add(types[^1].BaseType);
            
            return types;
        }
    }
}