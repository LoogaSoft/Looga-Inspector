using UnityEditor;
using UnityEngine;
using LoogaSoft.Inspector.Runtime;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(MinMaxSliderAttribute))]
    public class MinMaxSliderDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Vector2)
            {
                EditorGUI.LabelField(position, label.text, "MinMaxSlider is for Vector2's only");
                return;
            }

            MinMaxSliderAttribute rangeAttribute = (MinMaxSliderAttribute)attribute;

            Vector2 range = property.vector2Value;
            float min = range.x;
            float max = range.y;
            float minLimit = rangeAttribute.min;
            float maxLimit = rangeAttribute.max;

            int originalIndentLevel = EditorGUI.indentLevel;

            Rect contentPosition = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            EditorGUI.indentLevel = 0;

            const float fieldWidth = 50f;
            const float spacing = 5f;
            
            float sliderWidth = contentPosition.width - (fieldWidth * 2f) - (spacing * 2f);
            
            Rect minFieldRect = new Rect(contentPosition.x, contentPosition.y, fieldWidth, contentPosition.height);

            EditorGUI.BeginChangeCheck();
            min = EditorGUI.FloatField(minFieldRect, Mathf.Round(min * 100f) / 100f);

            if (EditorGUI.EndChangeCheck())
            {
                min = Mathf.Clamp(min, minLimit, maxLimit);
            }

            Rect sliderRect = new Rect(minFieldRect.xMax + spacing, contentPosition.y, sliderWidth,
                contentPosition.height);

            EditorGUI.BeginChangeCheck();
            EditorGUI.MinMaxSlider(sliderRect, ref min, ref max, minLimit, maxLimit);

            if (EditorGUI.EndChangeCheck())
            {
                min = Mathf.Clamp(min, minLimit, max);
                max = Mathf.Clamp(max, min, maxLimit);
            }

            Rect maxFieldRect = new Rect(sliderRect.xMax + spacing, contentPosition.y, fieldWidth,
                contentPosition.height);

            EditorGUI.BeginChangeCheck();
            max = EditorGUI.FloatField(maxFieldRect, Mathf.Round(max * 100f) / 100f);

            if (EditorGUI.EndChangeCheck())
            {
                max = Mathf.Clamp(max, min, maxLimit);
            }

            property.vector2Value = new Vector2(min, max);

            EditorGUI.indentLevel = originalIndentLevel;
        }
    }
}