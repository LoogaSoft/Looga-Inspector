using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public enum LoogaNoticeType
    {
        Info,
        Warning,
        Error
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
    public class NoticeAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string Message;
        public readonly LoogaNoticeType Type;

        public string Condition { get; set; } = string.Empty;
        public bool Invert { get; set; }
        public bool UseMember { get; set; }
        public string ButtonLabel { get; set; } = string.Empty;
        public string AssetPath { get; set; } = string.Empty;
        public string MenuPath { get; set; } = string.Empty;
        public string ActionTooltip { get; set; } = string.Empty;

        public NoticeAttribute(string message, LoogaNoticeType type = LoogaNoticeType.Info)
        {
            Message = message;
            Type = type;
        }
    }

    [Obsolete("Use LoogaNoticeType instead.")]
    public enum LoogaStatusBoxType
    {
        Info,
        Warning,
        Error
    }

    [Obsolete("Use NoticeAttribute instead.")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class StatusBoxAttribute : NoticeAttribute
    {
        public StatusBoxAttribute(string message, LoogaStatusBoxType type = LoogaStatusBoxType.Info)
            : base(message, (LoogaNoticeType)(int)type)
        {
        }
    }
}