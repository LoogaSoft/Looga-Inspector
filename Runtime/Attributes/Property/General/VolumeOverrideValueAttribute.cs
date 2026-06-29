using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class VolumeOverrideValueAttribute : PropertyAttribute, ILoogaAttribute
    {
        public readonly string volumeProfileMember;

        public VolumeOverrideValueAttribute(string volumeProfileMember)
        {
            this.volumeProfileMember = volumeProfileMember;
        }
    }
}
