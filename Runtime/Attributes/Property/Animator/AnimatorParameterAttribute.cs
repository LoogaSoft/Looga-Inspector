using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AnimatorParameterAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string animatorControllerName;


        public AnimatorParameterAttribute(string animatorControllerName)
        {
            this.animatorControllerName = animatorControllerName;
        }
    }
}