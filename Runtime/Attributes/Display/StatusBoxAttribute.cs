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

        public string Condition { get; set; }
        public bool Invert { get; set; }
        public bool UseMember { get; set; }

        public StatusBoxAttribute(string message, LoogaStatusBoxType type = LoogaStatusBoxType.Info)
        {
            Message = message;
            Type = type;
        }
    }
}
