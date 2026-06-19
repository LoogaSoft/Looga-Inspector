using System;
using UnityEngine;

namespace LoogaSoft.Inspector.Runtime
{
    public class ReadOnlyAttribute : Attribute, ILoogaAttribute
    {
    }

    public sealed class DisableInPlayModeAttribute : Attribute, ILoogaAttribute
    {
    }

    public sealed class DisableInEditModeAttribute : Attribute, ILoogaAttribute
    {
    }

    public sealed class ShowInPlayModeAttribute : Attribute, ILoogaAttribute
    {
    }

    public sealed class ShowInEditModeAttribute : Attribute, ILoogaAttribute
    {
    }
}
