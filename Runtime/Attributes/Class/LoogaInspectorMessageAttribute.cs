using System;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class LoogaInspectorMessageAttribute : Attribute, ILoogaAttribute
    {
        public readonly string Condition;
        public readonly string Message;
        public readonly MessageMode MessageMode;
        public readonly bool Invert;

        public LoogaInspectorMessageAttribute(
            string condition,
            string message,
            MessageMode messageMode = MessageMode.Info,
            bool invert = false)
        {
            Condition = condition;
            Message = message;
            MessageMode = messageMode;
            Invert = invert;
        }
    }
}
