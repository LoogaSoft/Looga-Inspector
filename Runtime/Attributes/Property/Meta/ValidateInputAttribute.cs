using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public enum MessageMode { Error, Warning, Info, None }
    public class ValidateInputAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string condition;
        public readonly string message;
        public readonly MessageMode messageMode;
        
        public ValidateInputAttribute(string condition, string message = "Invalid Input", MessageMode messageMode = MessageMode.Error)
        {
            this.condition = condition;
            this.message = message;
            this.messageMode = messageMode;
        }
    }
}