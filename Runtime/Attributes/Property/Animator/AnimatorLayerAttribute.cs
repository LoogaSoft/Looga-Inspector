using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AnimatorLayerAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string animatorControllerName;
        
        public AnimatorLayerAttribute(string animatorControllerName)
        {
            this.animatorControllerName = animatorControllerName;
        }
    }
}