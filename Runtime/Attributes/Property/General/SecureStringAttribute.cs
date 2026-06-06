using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class SecureStringAttribute : PropertyAttribute, ILoogaAttribute
    {
        public SecureStringAttribute(bool obscure = false)
        {
            Obscure = obscure;
        }

        public bool Obscure { get; }
    }
}
