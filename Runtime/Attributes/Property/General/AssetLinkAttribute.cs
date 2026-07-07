using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class AssetLinkAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly bool ReadOnly;
        public readonly bool ShowPingButton;

        public AssetLinkAttribute(bool readOnly = false, bool showPingButton = true)
        {
            ReadOnly = readOnly;
            ShowPingButton = showPingButton;
        }
    }
}
