using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public enum LoogaStatusBoxType
    {
        Info,
        Warning,
        Error
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Field, AllowMultiple = true)]
    public sealed class StatusBoxAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string Message;
        public readonly LoogaStatusBoxType Type;

        public string Condition { get; set; } = string.Empty;
        public bool Invert { get; set; }
        public bool UseMember { get; set; }
        public string ButtonLabel { get; set; } = string.Empty;
        public string AssetPath { get; set; } = string.Empty;
        public string MenuPath { get; set; } = string.Empty;

        public StatusBoxAttribute(string message, LoogaStatusBoxType type = LoogaStatusBoxType.Info)
        {
            Message = message;
            Type = type;
        }
    }
}
