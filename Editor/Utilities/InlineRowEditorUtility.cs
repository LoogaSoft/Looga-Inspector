using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class InlineRowEditorUtility
    {
        public static float SingleLineHeight => EditorGUIUtility.singleLineHeight;

        public static void DrawProperties(Rect rect, IReadOnlyList<SerializedProperty> properties, IReadOnlyList<GUIContent> labels)
        {
            DrawProperties(rect, properties, labels, null);
        }

        public static void DrawProperties(
            Rect rect,
            IReadOnlyList<SerializedProperty> properties,
            IReadOnlyList<GUIContent> labels,
            IReadOnlyList<float> weights)
        {
            if (properties == null || properties.Count == 0)
                return;

            float gap = 4f;
            float availableWidth = rect.width - gap * (properties.Count - 1);
            float totalWeight = 0f;
            for (int i = 0; i < properties.Count; i++)
                totalWeight += GetWeight(weights, i);

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            float x = rect.x;

            for (int i = 0; i < properties.Count; i++)
            {
                float width = availableWidth * (GetWeight(weights, i) / totalWeight);
                Rect fieldRect = new(x, rect.y, width, SingleLineHeight);
                GUIContent label = labels != null && i < labels.Count ? labels[i] : GUIContent.none;
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Mathf.Min(oldLabelWidth, Mathf.Max(42f, width * 0.35f));
                EditorGUI.PropertyField(fieldRect, properties[i], label, false);
                EditorGUIUtility.labelWidth = oldLabelWidth;
                x += width + gap;
            }

            EditorGUI.indentLevel = oldIndent;
        }

        private static float GetWeight(IReadOnlyList<float> weights, int index)
        {
            if (weights == null || index >= weights.Count)
                return 1f;

            return Mathf.Max(0.01f, weights[index]);
        }

        public static List<SerializedProperty> GetVisibleChildren(SerializedProperty property)
        {
            List<SerializedProperty> children = new();
            if (property == null || !property.hasVisibleChildren)
                return children;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty();
            int depth = iterator.depth;

            if (!iterator.NextVisible(true))
                return children;

            do
            {
                if (iterator.depth <= depth || SerializedProperty.EqualContents(iterator, end))
                    break;

                children.Add(iterator.Copy());
            } while (iterator.NextVisible(false));

            return children;
        }
    }
}
