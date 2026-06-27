using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaInspectorZLinqSupportMenu
    {
        private const string MenuPath = "LoogaSoft/Inspector/Enable ZLinq Support";
        private const string DefineSymbol = "LOOGA_INSPECTOR_ZLINQ_SUPPORT";

        private static readonly string[] RequiredAssemblies =
        {
            "ZLinq"
        };

        [MenuItem(MenuPath, priority = 204)]
        private static void ToggleZLinqSupport()
        {
            if (IsEnabled())
            {
                Disable();
                return;
            }

            if (!LoogaInspectorOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies))
            {
                EditorUtility.DisplayDialog("ZLinq Not Found", "Install ZLinq before enabling Looga Inspector ZLinq support.\n\nMissing: " + missingAssemblies, "OK");
                return;
            }

            Enable();
        }

        [MenuItem(MenuPath, true)]
        private static bool ValidateToggle()
        {
            UnityEditor.Menu.SetChecked(MenuPath, IsEnabled());
            return true;
        }

        private static bool IsEnabled()
        {
            return LoogaInspectorOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        private static void Enable()
        {
            LoogaInspectorOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga Inspector ZLinq support enabled.");
        }

        private static void Disable()
        {
            LoogaInspectorOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            AssetDatabase.Refresh();
            Debug.Log("Looga Inspector ZLinq support disabled.");
        }
    }
}