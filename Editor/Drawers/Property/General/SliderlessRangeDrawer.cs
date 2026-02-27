using System;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(SliderlessRangeAttribute))]
    public class SliderlessRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SliderlessRangeAttribute attr = (SliderlessRangeAttribute)attribute;

            label = EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            if (property.propertyType == SerializedPropertyType.Float)
            {
                if (fieldInfo.FieldType == typeof(double))
                {
                    double value = EditorGUI.DoubleField(position, label, property.doubleValue);
                    if (EditorGUI.EndChangeCheck())
                        property.doubleValue = Math.Clamp(value, attr.min, attr.max);
                }
                else
                {
                    float value = EditorGUI.FloatField(position, label, property.floatValue);
                    if (EditorGUI.EndChangeCheck())
                        property.floatValue = Mathf.Clamp(value, (float)attr.min, (float)attr.max);
                }
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                if (fieldInfo.FieldType == typeof(long))
                {
                    long value = EditorGUI.LongField(position, label, property.longValue);
                    if (EditorGUI.EndChangeCheck())
                        property.longValue = Math.Clamp(value, (long)attr.min, (long)attr.max);
                }
                else
                {
                    int value = EditorGUI.IntField(position, label, property.intValue);
                    if (EditorGUI.EndChangeCheck())
                        property.intValue = Mathf.Clamp(value, (int)attr.min, (int)attr.max);
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "SliderlessRange is for Floats and Integers only");
            }

            EditorGUI.EndProperty();
        }
    }
}
