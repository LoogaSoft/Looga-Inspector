using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class ReflectionUtils
    {
        private const BindingFlags MemberFlags = BindingFlags.Public
            | BindingFlags.NonPublic
            | BindingFlags.Instance
            | BindingFlags.DeclaredOnly
            | BindingFlags.Static;

        private static readonly Dictionary<Type, List<Type>> TypeHierarchyCache = new();
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new();
        private static readonly Dictionary<Type, PropertyInfo[]> PropertyCache = new();
        private static readonly Dictionary<Type, MethodInfo[]> MethodCache = new();
        private static readonly Dictionary<MemberLookupKey, FieldInfo> FieldLookupCache = new();
        private static readonly Dictionary<MemberLookupKey, PropertyInfo> PropertyLookupCache = new();
        private static readonly Dictionary<MemberLookupKey, MethodInfo> MethodLookupCache = new();

        public static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("Target cannot be null!");
                yield break;
            }

            FieldInfo[] fields = GetFields(target.GetType());
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (predicate == null || predicate(field))
                {
                    yield return field;
                }
            }
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(object target, Func<PropertyInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("Target cannot be null!");
                yield break;
            }

            PropertyInfo[] properties = GetProperties(target.GetType());
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (predicate == null || predicate(property))
                {
                    yield return property;
                }
            }
        }

        public static IEnumerable<MethodInfo> GetAllMethods(object target, Func<MethodInfo, bool> predicate)
        {
            if (target == null)
            {
                Debug.LogError("Target cannot be null!");
                yield break;
            }

            MethodInfo[] methods = GetMethods(target.GetType());
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (predicate == null || predicate(method))
                {
                    yield return method;
                }
            }
        }

        public static FieldInfo GetField(object target, string fieldName)
        {
            return target == null ? null : GetField(target.GetType(), fieldName);
        }

        public static PropertyInfo GetProperty(object target, string propertyName)
        {
            return target == null ? null : GetProperty(target.GetType(), propertyName);
        }

        public static MethodInfo GetMethod(object target, string methodName)
        {
            return target == null ? null : GetMethod(target.GetType(), methodName);
        }

        public static FieldInfo GetField(Type type, string fieldName)
        {
            if (type == null || string.IsNullOrWhiteSpace(fieldName))
            {
                return null;
            }

            MemberLookupKey key = new(type, fieldName);
            if (FieldLookupCache.TryGetValue(key, out FieldInfo cachedField))
            {
                return cachedField;
            }

            FieldInfo[] fields = GetFields(type);
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].Name == fieldName)
                {
                    FieldLookupCache[key] = fields[i];
                    return fields[i];
                }
            }

            FieldLookupCache[key] = null;
            return null;
        }

        public static PropertyInfo GetProperty(Type type, string propertyName)
        {
            if (type == null || string.IsNullOrWhiteSpace(propertyName))
            {
                return null;
            }

            MemberLookupKey key = new(type, propertyName);
            if (PropertyLookupCache.TryGetValue(key, out PropertyInfo cachedProperty))
            {
                return cachedProperty;
            }

            PropertyInfo[] properties = GetProperties(type);
            for (int i = 0; i < properties.Length; i++)
            {
                if (properties[i].Name == propertyName)
                {
                    PropertyLookupCache[key] = properties[i];
                    return properties[i];
                }
            }

            PropertyLookupCache[key] = null;
            return null;
        }

        public static MethodInfo GetMethod(Type type, string methodName)
        {
            if (type == null || string.IsNullOrWhiteSpace(methodName))
            {
                return null;
            }

            MemberLookupKey key = new(type, methodName);
            if (MethodLookupCache.TryGetValue(key, out MethodInfo cachedMethod))
            {
                return cachedMethod;
            }

            MethodInfo[] methods = GetMethods(type);
            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].Name == methodName)
                {
                    MethodLookupCache[key] = methods[i];
                    return methods[i];
                }
            }

            MethodLookupCache[key] = null;
            return null;
        }

        private static FieldInfo[] GetFields(Type type)
        {
            if (FieldCache.TryGetValue(type, out FieldInfo[] fields))
            {
                return fields;
            }

            List<FieldInfo> result = new();
            List<Type> types = GetTypeHierarchy(type);
            for (int i = types.Count - 1; i >= 0; i--)
            {
                result.AddRange(types[i].GetFields(MemberFlags));
            }

            fields = result.ToArray();
            FieldCache[type] = fields;
            return fields;
        }

        private static PropertyInfo[] GetProperties(Type type)
        {
            if (PropertyCache.TryGetValue(type, out PropertyInfo[] properties))
            {
                return properties;
            }

            List<PropertyInfo> result = new();
            List<Type> types = GetTypeHierarchy(type);
            for (int i = types.Count - 1; i >= 0; i--)
            {
                result.AddRange(types[i].GetProperties(MemberFlags));
            }

            properties = result.ToArray();
            PropertyCache[type] = properties;
            return properties;
        }

        private static MethodInfo[] GetMethods(Type type)
        {
            if (MethodCache.TryGetValue(type, out MethodInfo[] methods))
            {
                return methods;
            }

            List<MethodInfo> result = new();
            List<Type> types = GetTypeHierarchy(type);
            for (int i = types.Count - 1; i >= 0; i--)
            {
                result.AddRange(types[i].GetMethods(MemberFlags));
            }

            methods = result.ToArray();
            MethodCache[type] = methods;
            return methods;
        }

        private static List<Type> GetTypeHierarchy(Type type)
        {
            if (TypeHierarchyCache.TryGetValue(type, out List<Type> cachedTypes))
            {
                return cachedTypes;
            }

            List<Type> types = new();
            while (type != null)
            {
                types.Add(type);
                type = type.BaseType;
            }

            TypeHierarchyCache[types[0]] = types;
            return types;
        }

        private readonly struct MemberLookupKey : IEquatable<MemberLookupKey>
        {
            private readonly Type _type;
            private readonly string _name;

            public MemberLookupKey(Type type, string name)
            {
                _type = type;
                _name = name;
            }

            public bool Equals(MemberLookupKey other)
            {
                return _type == other._type && string.Equals(_name, other._name, StringComparison.Ordinal);
            }

            public override bool Equals(object obj)
            {
                return obj is MemberLookupKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((_type != null ? _type.GetHashCode() : 0) * 397)
                        ^ (_name != null ? StringComparer.Ordinal.GetHashCode(_name) : 0);
                }
            }
        }
    }
}
