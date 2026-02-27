using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string label;
        public bool drawAtTop;
        
        public ButtonAttribute(string label = null, bool drawAtTop = false)
        {
            this.label = label;
            this.drawAtTop = drawAtTop;
        }
    }
}