using System.Reflection;
using LoogaSoft.Inspector.Runtime;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public struct InspectorButton
    {
        public MethodInfo method;
        public string label;
        public bool drawAtTop;
        public string enableIf;
        public string confirmMessage;
        public float height;
        public LoogaButtonMode mode;
    }
}
