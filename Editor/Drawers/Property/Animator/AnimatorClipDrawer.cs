using System.Collections.Generic;
using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEngine;
#if ZLINQ_SUPPORT
using ZLinq;
#else
using System.Linq;
#endif

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(AnimatorClipAttribute))]
    public class AnimatorClipDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            var acAttribute = (AnimatorClipAttribute)attribute;
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.LabelField(position, label.text, "AnimatorClipAttribute can only be used with strings");
                EditorGUI.EndProperty();
                return;
            }
            
            var controller = AnimatorHelper.GetAnimatorController(property, acAttribute.animatorControllerName);
            
            if (controller == null)
            {
                EditorGUI.LabelField(position, label.text, "Animator Controller not found");
                return;
            }
            
            List<string> clipNames = controller.animationClips
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .Select(c => c.name)
                .ToList();
            
            clipNames.Insert(0, "None");
            
            var currentIndex = Mathf.Max(0, clipNames.IndexOf(property.stringValue));
            if (currentIndex < 0) currentIndex = 0;
            
            var newIndex = EditorGUI.Popup(position, label.text, currentIndex, clipNames.ToArray());
            
            if (newIndex != currentIndex) 
                property.stringValue = clipNames[newIndex];
            
            EditorGUI.EndProperty();
        }
    }
}