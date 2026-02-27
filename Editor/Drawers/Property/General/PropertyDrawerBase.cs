using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public abstract class PropertyDrawerBase : PropertyDrawer
    {
        public sealed override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            bool visible = PropertyUtils.IsVisible(property);
            if (!visible)
                return;
            
            EditorGUI.BeginChangeCheck();
            
            bool enabled = PropertyUtils.IsEnabled(property);

            //disable GUI if enabled is false
            using (new EditorGUI.DisabledScope(disabled: !enabled))
                OnGUI_Internal(position, property, PropertyUtils.GetLabel(property));
            
            if (EditorGUI.EndChangeCheck())
                PropertyUtils.CallOnFieldChangedCallbacks(property);
        }
        protected abstract void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label);

        public sealed override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            bool visible = PropertyUtils.IsVisible(property);
            if (!visible)
                return 0f;
            
            return GetPropertyHeight_Internal(property, label);
        }

        protected virtual float GetPropertyHeight_Internal(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, includeChildren: true);
        }

    }
}