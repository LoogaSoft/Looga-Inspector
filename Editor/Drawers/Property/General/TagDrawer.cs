using LoogaSoft.Inspector.Runtime;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

#if ZLINQ_SUPPORT
using ZLinq;
#else
using System.Linq;
#endif

namespace LoogaSoft.Inspector.Editor
{
    [CustomPropertyDrawer(typeof(TagAttribute))]
    public class TagDrawer : PropertyDrawerBase
    {
        protected override void OnGUI_Internal(Rect position, SerializedProperty property, GUIContent label)
        {
            var tagsList = InternalEditorUtility.tags
                #if ZLINQ_SUPPORT
                .AsValueEnumerable()
                #endif
                .ToList();
            
            tagsList.Insert(0, "None");
            
            EditorGUI.BeginProperty(position, label, property);

            if (property.propertyType == SerializedPropertyType.String)
            {
                var currentIndex = Mathf.Max(0, tagsList.IndexOf(property.stringValue));
                var newIndex = EditorGUI.Popup(position, label.text, currentIndex, tagsList.ToArray());
                property.stringValue = tagsList[newIndex];
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use TagAttribute with strings only");
            }
            
            EditorGUI.EndProperty();
        }
    }
}