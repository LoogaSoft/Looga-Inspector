using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class AnimatorStateAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string animatorControllerName;
        
        public AnimatorStateAttribute(string animatorControllerName)
        {
            this.animatorControllerName = animatorControllerName;
        }
    }
}