using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(TableListAttribute))]
    public sealed class TableListDrawer : PropertyDrawerBase
    {
        private const float RowGap = 2f;
        private const float ButtonWidth = 24f;

        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            TableListAttribute tableAttribute = (TableListAttribute)attribute;
            if (!CanDrawTable(property, tableAttribute))
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect labelRect = new(position.x, position.y, position.width, lineHeight);
            property.isExpanded = EditorGUI.Foldout(labelRect, property.isExpanded, label, true);

            if (!property.isExpanded)
                return;

            int oldIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect tableRect = EditorGUI.IndentedRect(new Rect(
                position.x,
                labelRect.yMax + RowGap,
                position.width,
                position.height - lineHeight - RowGap));

            Rect headerRect = new(tableRect.x, tableRect.y, tableRect.width, lineHeight);
            DrawHeader(headerRect, tableAttribute);

            float y = headerRect.yMax + RowGap;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty element = property.GetArrayElementAtIndex(i);
                Rect rowRect = new(tableRect.x, y, tableRect.width, lineHeight);
                DrawRow(rowRect, element, tableAttribute);
                y += lineHeight + RowGap;
            }

            if (tableAttribute.allowAddRemove)
                DrawAddRemoveButtons(new Rect(tableRect.x, y, tableRect.width, lineHeight), property);

            EditorGUI.indentLevel = oldIndent;
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            TableListAttribute tableAttribute = (TableListAttribute)attribute;
            if (!CanDrawTable(property, tableAttribute))
                return EditorGUI.GetPropertyHeight(property, label, true);

            float lineHeight = EditorGUIUtility.singleLineHeight;
            if (!property.isExpanded)
                return lineHeight;

            int rowCount = property.arraySize + 1 + (tableAttribute.allowAddRemove ? 1 : 0);
            return lineHeight + RowGap + rowCount * lineHeight + Mathf.Max(0, rowCount - 1) * RowGap;
        }

        private static bool CanDrawTable(SerializedProperty property, TableListAttribute tableAttribute)
        {
            return property.isArray
                && property.propertyType != SerializedPropertyType.String
                && tableAttribute.columns != null
                && tableAttribute.columns.Length > 0;
        }

        private static void DrawHeader(Rect rect, TableListAttribute tableAttribute)
        {
            Rect[] columns = GetColumnRects(rect, tableAttribute.columns.Length);
            for (int i = 0; i < tableAttribute.columns.Length; i++)
            {
                string header = ObjectNames.NicifyVariableName(tableAttribute.columns[i]);
                EditorGUI.LabelField(columns[i], header, EditorStyles.boldLabel);
            }
        }

        private static void DrawRow(Rect rect, SerializedProperty element, TableListAttribute tableAttribute)
        {
            Rect[] columns = GetColumnRects(rect, tableAttribute.columns.Length);
            for (int i = 0; i < tableAttribute.columns.Length; i++)
            {
                SerializedProperty columnProperty = element.FindPropertyRelative(tableAttribute.columns[i]);
                if (columnProperty == null)
                {
                    EditorGUI.LabelField(columns[i], "-");
                    continue;
                }

                EditorGUI.PropertyField(columns[i], columnProperty, GUIContent.none, false);
            }
        }

        private static void DrawAddRemoveButtons(Rect rect, SerializedProperty property)
        {
            Rect addRect = new(rect.xMax - ButtonWidth * 2f - RowGap, rect.y, ButtonWidth, rect.height);
            Rect removeRect = new(rect.xMax - ButtonWidth, rect.y, ButtonWidth, rect.height);

            if (GUI.Button(addRect, "+"))
                property.arraySize++;

            using (new EditorGUI.DisabledScope(property.arraySize == 0))
            {
                if (GUI.Button(removeRect, "-"))
                    property.arraySize--;
            }
        }

        private static Rect[] GetColumnRects(Rect rect, int columnCount)
        {
            Rect[] columns = new Rect[columnCount];
            float totalGap = RowGap * Mathf.Max(0, columnCount - 1);
            float width = (rect.width - totalGap) / columnCount;

            for (int i = 0; i < columnCount; i++)
                columns[i] = new Rect(rect.x + i * (width + RowGap), rect.y, width, rect.height);

            return columns;
        }
    }
}
