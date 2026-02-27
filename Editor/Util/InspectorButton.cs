using System.Reflection;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public struct InspectorButton
    {
        public MethodInfo method;
        public string label;
        public bool drawAtTop;
    }
}