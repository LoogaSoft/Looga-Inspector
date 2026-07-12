using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(NoticeAttribute))]
    public sealed class NoticeDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            NoticeAttribute noticeAttribute = (NoticeAttribute)attribute;
            float noticeHeight = GetNoticeHeight(property, noticeAttribute);
            if (noticeHeight > 0f)
            {
                Rect noticeRect = new(position.x, position.y, position.width, noticeHeight);
                LoogaGUI.Notice(noticeRect, ResolveMessage(property, noticeAttribute), noticeAttribute.Type);
                position.y += noticeHeight + EditorGUIUtility.standardVerticalSpacing;
                position.height -= noticeHeight + EditorGUIUtility.standardVerticalSpacing;
            }

            EditorGUI.PropertyField(position, property, label, true);
        }

        protected override float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUI.GetPropertyHeight(property, label, true);
            float noticeHeight = GetNoticeHeight(property, (NoticeAttribute)attribute);
            return noticeHeight <= 0f ? height : height + noticeHeight + EditorGUIUtility.standardVerticalSpacing;
        }

        internal static MessageType ToMessageType(LoogaNoticeType type)
        {
            return type switch
            {
                LoogaNoticeType.Warning => MessageType.Warning,
                LoogaNoticeType.Error => MessageType.Error,
                _ => MessageType.Info
            };
        }

        internal static bool ShouldShow(object target, NoticeAttribute noticeAttribute)
        {
            if (noticeAttribute == null)
                return false;

            if (string.IsNullOrWhiteSpace(noticeAttribute.Condition))
                return true;

            bool condition = PropertyUtils.GetConditionValue(target, noticeAttribute.Condition);
            return noticeAttribute.Invert ? !condition : condition;
        }

        internal static string ResolveMessage(SerializedProperty property, NoticeAttribute noticeAttribute)
        {
            return ResolveMessage(PropertyUtils.GetTargetObjectWithProperty(property), noticeAttribute);
        }

        internal static string ResolveMessage(object target, NoticeAttribute noticeAttribute)
        {
            if (noticeAttribute == null)
                return string.Empty;

            if (!noticeAttribute.UseMember || target == null || string.IsNullOrWhiteSpace(noticeAttribute.Message))
                return noticeAttribute.Message ?? string.Empty;

            object value = LoogaMemberValueUtility.GetValue(target, noticeAttribute.Message);
            return value?.ToString() ?? string.Empty;
        }

        private static float GetNoticeHeight(SerializedProperty property, NoticeAttribute noticeAttribute)
        {
            object target = PropertyUtils.GetTargetObjectWithProperty(property);
            if (!ShouldShow(target, noticeAttribute))
                return 0f;

            string message = ResolveMessage(target, noticeAttribute);
            return string.IsNullOrWhiteSpace(message) ? 0f : LoogaGUI.GetNoticeHeight(message);
        }
    }
}