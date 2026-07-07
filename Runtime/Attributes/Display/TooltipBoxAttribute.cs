using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public enum TooltipType { Info, Warning, Error }
    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class TooltipBoxAttribute : PropertyAttribute , ILoogaAttribute
    {
        public string tooltip;
        public TooltipType type;
        
        public TooltipBoxAttribute(string tooltip, TooltipType type = TooltipType.Info)
        {
            this.tooltip = tooltip;
            this.type = type;
        }
    }
}