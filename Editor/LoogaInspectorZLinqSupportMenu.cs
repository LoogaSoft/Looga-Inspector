using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaInspectorZLinqSupportMenu
    {
        private const string MenuPath = "LoogaSoft/Inspector/Enable ZLinq Support";
        private const string DefineSymbol = "LOOGA_INSPECTOR_ZLINQ_SUPPORT";
        private const string EditorAsmdef = "LoogaSoft.Inspector.Editor";

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
            return LoogaInspectorOptionalSupportUtility.DefineIsEnabled(DefineSymbol)
                && LoogaInspectorOptionalSupportUtility.AsmdefReferences(EditorAsmdef, "ZLinq");
        }

        private static void Enable()
        {
            LoogaInspectorOptionalSupportUtility.AddDefineSymbol(DefineSymbol);
            if (!LoogaInspectorOptionalSupportUtility.SetAsmdefReferences(EditorAsmdef, RequiredAssemblies, include: true, out string error))
                EditorUtility.DisplayDialog("Unable To Update Looga Inspector", error, "OK");

            AssetDatabase.Refresh();
            Debug.Log("Looga Inspector ZLinq support enabled.");
        }

        private static void Disable()
        {
            LoogaInspectorOptionalSupportUtility.RemoveDefineSymbol(DefineSymbol);
            if (!LoogaInspectorOptionalSupportUtility.SetAsmdefReferences(EditorAsmdef, RequiredAssemblies, include: false, out string error))
                EditorUtility.DisplayDialog("Unable To Update Looga Inspector", error, "OK");

            AssetDatabase.Refresh();
            Debug.Log("Looga Inspector ZLinq support disabled.");
        }
    }
}