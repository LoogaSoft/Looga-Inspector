using UnityEditor;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaInspectorZLinqSupportProvider
    {
        private const string DefineSymbol = "LOOGA_INSPECTOR_ZLINQ_SUPPORT";

        private static readonly string[] RequiredAssemblies =
        {
            "ZLinq"
        };

        public static string ProviderId => "looga-inspector.zlinq";
        public static string PackageName => "Looga Inspector";
        public static string IntegrationName => "ZLinq";
        public static string Description => "Uses ZLinq for allocation-conscious editor collection queries.";

        public static bool IsEnabled()
        {
            return LoogaInspectorOptionalSupportUtility.DefineIsEnabled(DefineSymbol);
        }

        public static string GetUnavailableReason()
        {
            return LoogaInspectorOptionalSupportUtility.AllAssembliesAreAvailable(RequiredAssemblies, out string missingAssemblies)
                ? string.Empty
                : "Install ZLinq. Missing assemblies: " + missingAssemblies;
        }

        public static void SetEnabled(bool enabled)
        {
            if (enabled)
                Enable();
            else
                Disable();
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
