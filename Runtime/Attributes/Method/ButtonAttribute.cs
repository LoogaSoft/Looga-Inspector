using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public enum LoogaButtonMode
    {
        Always,
        EditModeOnly,
        PlayModeOnly
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string label;
        public bool drawAtTop;
        public readonly string enableIf;
        public readonly string confirmMessage;
        public readonly float height;
        public readonly LoogaButtonMode mode;
        
        public ButtonAttribute(
            string label = null,
            bool drawAtTop = false,
            string enableIf = null,
            string confirmMessage = null,
            float height = 30f,
            LoogaButtonMode mode = LoogaButtonMode.Always)
        {
            this.label = label;
            this.drawAtTop = drawAtTop;
            this.enableIf = enableIf;
            this.confirmMessage = confirmMessage;
            this.height = height;
            this.mode = mode;
        }
    }
}
