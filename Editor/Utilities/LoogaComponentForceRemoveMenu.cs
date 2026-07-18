using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaComponentForceRemoveMenu
    {
        const string MenuPath = "CONTEXT/Component/Force Remove (With Dependents)";

        [MenuItem(MenuPath, true)]
        static bool CanForceRemove(MenuCommand command)
        {
            return command.context is Component component
                   && component != null
                   && component is not Transform
                   && CollectRemovalOrder(component).Count > 1;
        }

        [MenuItem(MenuPath, false, 1000)]
        static void ForceRemove(MenuCommand command)
        {
            if (command.context is not Component component || component == null)
            {
                return;
            }

            List<Component> removalOrder = CollectRemovalOrder(component);
            if (removalOrder.Count <= 1)
            {
                return;
            }

            string componentName = GetComponentDisplayName(component);
            string message = BuildConfirmationMessage(componentName, removalOrder);
            if (!EditorUtility.DisplayDialog("Force Remove Component", message, "Remove", "Cancel"))
            {
                return;
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"Force Remove {componentName}");
            for (int i = 0; i < removalOrder.Count; i++)
            {
                Component target = removalOrder[i];
                if (target != null)
                {
                    Undo.DestroyObjectImmediate(target);
                }
            }

            Undo.CollapseUndoOperations(undoGroup);
        }

        static List<Component> CollectRemovalOrder(Component component)
        {
            var removalOrder = new List<Component>();
            var included = new HashSet<Component>();
            AddDependentsBefore(component, included, removalOrder);
            included.Add(component);
            removalOrder.Add(component);
            return removalOrder;
        }

        static void AddDependentsBefore(Component component, HashSet<Component> included, List<Component> removalOrder)
        {
            GameObject gameObject = component.gameObject;
            Component[] components = gameObject.GetComponents<Component>();
            Type componentType = component.GetType();

            for (int i = 0; i < components.Length; i++)
            {
                Component candidate = components[i];
                if (candidate == null || candidate == component || included.Contains(candidate))
                {
                    continue;
                }

                if (!RequiresComponent(candidate.GetType(), componentType))
                {
                    continue;
                }

                AddDependentsBefore(candidate, included, removalOrder);
                if (included.Add(candidate))
                {
                    removalOrder.Add(candidate);
                }
            }
        }

        static bool RequiresComponent(Type dependentType, Type requiredComponentType)
        {
            object[] attributes = dependentType.GetCustomAttributes(typeof(RequireComponent), true);
            for (int i = 0; i < attributes.Length; i++)
            {
                if (attributes[i] is not RequireComponent requireComponent)
                {
                    continue;
                }

                if (MatchesRequiredType(requireComponent.m_Type0, requiredComponentType)
                    || MatchesRequiredType(requireComponent.m_Type1, requiredComponentType)
                    || MatchesRequiredType(requireComponent.m_Type2, requiredComponentType))
                {
                    return true;
                }
            }

            return false;
        }

        static bool MatchesRequiredType(Type requiredType, Type componentType)
        {
            return requiredType != null && requiredType.IsAssignableFrom(componentType);
        }

        static string BuildConfirmationMessage(string componentName, List<Component> removalOrder)
        {
            var builder = new StringBuilder();
            builder.Append("Remove ");
            builder.Append(componentName);
            builder.Append(" and ");
            builder.Append(removalOrder.Count - 1);
            builder.Append(removalOrder.Count == 2 ? " dependent component?" : " dependent components?");
            builder.AppendLine();
            builder.AppendLine();

            for (int i = 0; i < removalOrder.Count; i++)
            {
                builder.Append("- ");
                builder.AppendLine(GetComponentDisplayName(removalOrder[i]));
            }

            return builder.ToString();
        }

        static string GetComponentDisplayName(Component component)
        {
            return component == null ? "Missing Component" : ObjectNames.NicifyVariableName(component.GetType().Name);
        }
    }
}
