using UnityEngine;
using Object = UnityEngine.Object;

namespace LoogaSoft.Inspector.Editor
{
    /// <summary>
    /// Centralizes whether Looga Inspector may expose mutating editor controls for an inspected target.
    /// Unity's NotEditable flag is intentionally treated as a package-agnostic read-only contract.
    /// </summary>
    internal static class LoogaInspectorTargetUtility
    {
        public static bool IsMutable(Object target)
        {
            if (target == null || HasNotEditableFlag(target))
                return false;

            GameObject gameObject = target switch
            {
                GameObject targetGameObject => targetGameObject,
                Component component => component.gameObject,
                _ => null
            };

            return gameObject == null || !HasNotEditableFlag(gameObject);
        }

        public static bool AreMutable(Object[] targets)
        {
            if (targets == null || targets.Length == 0)
                return false;

            for (int i = 0; i < targets.Length; i++)
            {
                if (!IsMutable(targets[i]))
                    return false;
            }

            return true;
        }

        private static bool HasNotEditableFlag(Object target)
        {
            return (target.hideFlags & HideFlags.NotEditable) != 0;
        }
    }
}
