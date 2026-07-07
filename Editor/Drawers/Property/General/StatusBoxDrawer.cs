using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(StatusBoxAttribute))]
    public sealed class StatusBoxDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            StatusBoxAttribute statusAttribute = (StatusBoxAttribute)attribute;
            float helpHeight = GetStatusHeight(property, statusAttribute);
            if (helpHeight > 0f)
            {
                Rect helpRect = new(position.x, position.y, position.width, helpHeight);
                EditorGUI.HelpBox(helpRect, ResolveMessage(property, statusAttribute), ToMessageType(statusAttribute.Type));
                position.y += helpHeight + EditorGUIUtility.standardVerticalSpacing;
                position.height -= helpHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true);
            float helpHeight = GetStatusHeight(property, (StatusBoxAttribute)attribute);
            return helpHeight <= 0f ? height : height + helpHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        internal static MessageType ToMessageType(LoogaStatusBoxType type)
        {
            return type switch
            {
                LoogaStatusBoxType.Warning => MessageType.Warning,
                LoogaStatusBoxType.Error => MessageType.Error,
                _ => MessageType.Info
            };
        }

        internal static bool ShouldShow(object target, StatusBoxAttribute statusAttribute)
        {
            if (statusAttribute == null)
                return false;

            if (string.IsNullOrWhiteSpace(statusAttribute.Condition))
                return true;

            bool condition = PropertyUtils.GetConditionValue(target, statusAttribute.Condition);
            return statusAttribute.Invert ? !condition : condition;
        }

        internal static string ResolveMessage(SerializedProperty property, StatusBoxAttribute statusAttribute)
        {
            return ResolveMessage(PropertyUtils.GetTargetObjectWithProperty(property), statusAttribute);
        }

        internal static string ResolveMessage(object target, StatusBoxAttribute statusAttribute)
        {
            if (statusAttribute == null)
                return string.Empty;

            if (!statusAttribute.UseMember || target == null || string.IsNullOrWhiteSpace(statusAttribute.Message))
                return statusAttribute.Message ?? string.Empty;

            object value = LoogaMemberValueUtility.GetValue(target, statusAttribute.Message);
            return value?.ToString() ?? string.Empty;
        }

        private static float GetStatusHeight(SerializedProperty property, StatusBoxAttribute statusAttribute)
        {
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            if (!ShouldShow(target, statusAttribute))
                return 0f;

            string message = ResolveMessage(target, statusAttribute);
            return string.IsNullOrWhiteSpace(message) ? 0f : EditorGUIUtility.singleLineHeight * 2.1f;
        }
    }
}
