using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections;
using LoogaSoft.Inspector.Runtime;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(BoolButtonAttribute))]
    public class BoolButtonDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var buttonAttribute = (BoolButtonAttribute)attribute;

            if (GUI.Button(position, buttonAttribute.buttonLabel))
            {
                //get the parent object of the property, which should be the object containing the field
                var parentObject = GetTargetObjectOfProperty(property);

                if (parentObject == null)
                    return;

                //find the method using reflection, using the parent object's type and the specified method name
                var method = FindMethod(parentObject.GetType(), buttonAttribute.methodName);

                //if the method is found, invoke it on the parent object
                //otherwise, log an error
                if (method != null)
                    method.Invoke(parentObject, null);
                else
                    Debug.LogError($"Could not find method {buttonAttribute.methodName} on object {parentObject}");
            }
        }

        private static MethodInfo FindMethod(Type type, string methodName)
        {
            //run a check for the method using specified type
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                        BindingFlags.DeclaredOnly;
            var method = type.GetMethod(methodName, flags);

            //if the method is found, return it
            if (method != null || type.BaseType == null || type.BaseType == typeof(object))
                return method;

            //run another check for the method on the base type
            flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            method = type.BaseType.GetMethod(methodName, flags);

            //if the method is found, return it
            return method;
        }

        private static object GetTargetObjectOfProperty(SerializedProperty prop)
        {
            if (prop == null)
                return null;

            //modify property path
            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            string[] elements = path.Split('.');

            //iterate through elements to get to the child object we want
            for (int i = 0; i < elements.Length - 1; i++)
            {
                string element = elements[i];

                if (element.Contains("["))
                {
                    //if the element contains an array index, get the object inside the array using a substring and TypeConverter
                    var fieldName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    var index = Convert.ToInt32(
                        element.Substring(element.IndexOf("[", StringComparison.Ordinal))
                            .Replace("[", "")
                            .Replace("]", "")
                    );

                    obj = GetValue(obj, fieldName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }

                if (obj == null) return null;
            }

            return obj;
        }

        private static object GetValue(object source, string name)
        {
            if (source == null)
                return null;

            //try getting the field using the source type and specified flags
            var type = source.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            var field = type.GetField(name, flags);

            //if the field is found, return its value
            if (field != null)
                return field.GetValue(source);

            //otherwise, try getting the property using the source type and specified flags
            var property = type.GetProperty(name, flags);
            if (property != null)
                return property.GetValue(source, null);

            return null;
        }

        private static object GetValue(object source, string name, int index)
        {
            //try casting above GetValue to IEnumerable, return null if not valid
            if (GetValue(source, name) is not IEnumerable enumerable)
                return null;

            //iterate through the enumerable until the index is reached, return null if not found
            IEnumerator enumerator = null;
            try
            {
                //use a for loop to avoid using MoveNext() multiple times, which can be expensive
                enumerator = enumerable.GetEnumerator();
                for (var i = 0; i <= index; i++)
                {
                    if (!enumerator.MoveNext())
                        return null;
                }

                return enumerator.Current;
            }
            finally
            {
                //ensure the enumerator is disposed of, even if an exception is thrown
                if (enumerator is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}