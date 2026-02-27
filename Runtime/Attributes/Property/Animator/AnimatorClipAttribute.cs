using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class AnimatorClipAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string animatorControllerName;

        public AnimatorClipAttribute(string animatorControllerName)
        {
            this.animatorControllerName = animatorControllerName;
        }
    }
}