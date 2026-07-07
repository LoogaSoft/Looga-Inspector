using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public class AnimatorParameterAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string animatorControllerName;
        public readonly bool filterByParameterType;
        public readonly AnimatorControllerParameterType parameterType;

        public AnimatorParameterAttribute(string animatorControllerName)
        {
            this.animatorControllerName = animatorControllerName;
        }

        public AnimatorParameterAttribute(string animatorControllerName, AnimatorControllerParameterType parameterType)
        {
            this.animatorControllerName = animatorControllerName;
            this.parameterType = parameterType;
            filterByParameterType = true;
        }
    }
}
