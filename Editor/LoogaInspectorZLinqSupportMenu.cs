using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaInspectorZLinqSupportMenu
    {
        private const string MenuPath = "LoogaSoft/Inspector/Enable ZLinq Support";
        private const string DefineSymbol = "ZLINQ_SUPPORT";
        private const string ZLinqAssemblyName = "ZLinq";
        private const string InspectorEditorAssemblyName = "LoogaSoft.Inspector.Editor";

        [MenuItem(MenuPath, priority = 204)]
        private static void ToggleZLinqSupport()
        {
            if (IsEnabled())
            {
                Disable();
                return;
            }

            if (!AssemblyIsAvailable(ZLinqAssemblyName))
            {
                EditorUtility.DisplayDialog("ZLinq Not Found", "Install ZLinq before enabling Looga Inspector ZLinq support.", "OK");
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
            return DefineIsEnabled() && InspectorAsmdefReferencesZLinq();
        }

        private static void Enable()
        {
            AddDefineSymbol(DefineSymbol);
            if (!TrySetInspectorAsmdefReference(includeZLinq: true, out string error))
                EditorUtility.DisplayDialog("Unable To Update Looga Inspector", error, "OK");

            AssetDatabase.Refresh();
            Debug.Log("Looga Inspector ZLinq support enabled.");
        }

        private static void Disable()
        {
            RemoveDefineSymbol(DefineSymbol);
            if (!TrySetInspectorAsmdefReference(includeZLinq: false, out string error))
                EditorUtility.DisplayDialog("Unable To Update Looga Inspector", error, "OK");

            AssetDatabase.Refresh();
            Debug.Log("Looga Inspector ZLinq support disabled.");
        }

        private static bool AssemblyIsAvailable(string assemblyName)
        {
            if (CompilationPipeline.GetAssemblies().Any(assembly => assembly.name == assemblyName))
                return true;

            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == assemblyName))
                return true;

            return AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset").Length > 0;
        }

        private static bool InspectorAsmdefReferencesZLinq()
        {
            if (!TryGetInspectorAsmdefPath(out string path))
                return false;

            string json = File.ReadAllText(path);
            return json.Contains($@"""{ZLinqAssemblyName}""");
        }

        private static bool TrySetInspectorAsmdefReference(bool includeZLinq, out string error)
        {
            error = string.Empty;

            if (!TryGetInspectorAsmdefPath(out string path))
            {
                error = "Could not find LoogaSoft.Inspector.Editor.asmdef.";
                return false;
            }

            try
            {
                string json = File.ReadAllText(path);
                InspectorAsmdef asmdef = JsonUtility.FromJson<InspectorAsmdef>(json);
                List<string> references = asmdef.references != null
                    ? asmdef.references.ToList()
                    : new List<string>();

                bool changed;
                if (includeZLinq)
                {
                    changed = !references.Contains(ZLinqAssemblyName);
                    if (changed)
                        references.Add(ZLinqAssemblyName);
                }
                else
                {
                    changed = references.RemoveAll(reference => reference == ZLinqAssemblyName) > 0;
                }

                if (changed)
                {
                    asmdef.references = references.ToArray();
                    File.WriteAllText(path, JsonUtility.ToJson(asmdef, prettyPrint: true));
                }

                return true;
            }
            catch (Exception exception)
            {
                error = "Could not update the Looga Inspector editor asmdef. If this package is installed from an immutable PackageCache location, embed the package or edit the source package before enabling ZLinq support.\n\n" + exception.Message;
                return false;
            }
        }

        private static bool TryGetInspectorAsmdefPath(out string path)
        {
            string[] guids = AssetDatabase.FindAssets($"{InspectorEditorAssemblyName} t:AssemblyDefinitionAsset");
            foreach (string guid in guids)
            {
                string candidate = AssetDatabase.GUIDToAssetPath(guid);
                if (candidate.EndsWith("LoogaSoft.Inspector.Editor.asmdef", StringComparison.Ordinal))
                {
                    path = candidate;
                    return true;
                }
            }

            path = string.Empty;
            return false;
        }

        private static bool DefineIsEnabled()
        {
            return PlayerSettings
                .GetScriptingDefineSymbols(GetNamedBuildTarget())
                .Split(';')
                .Any(symbol => symbol == DefineSymbol);
        }

        private static void AddDefineSymbol(string defineSymbol)
        {
            List<string> defines = GetDefines();
            if (defines.Contains(defineSymbol))
                return;

            defines.Add(defineSymbol);
            ApplyDefineSymbols(defines);
        }

        private static void RemoveDefineSymbol(string defineSymbol)
        {
            List<string> defines = GetDefines();
            if (!defines.Remove(defineSymbol))
                return;

            ApplyDefineSymbols(defines);
        }

        private static List<string> GetDefines()
        {
            return PlayerSettings.GetScriptingDefineSymbols(GetNamedBuildTarget())
                .Split(';')
                .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
                .Distinct()
                .ToList();
        }

        private static void ApplyDefineSymbols(List<string> defineSymbols)
        {
            string newDefines = string.Join(";", defineSymbols.Distinct().ToArray());
            PlayerSettings.SetScriptingDefineSymbols(GetNamedBuildTarget(), newDefines);
        }

        private static NamedBuildTarget GetNamedBuildTarget()
        {
            BuildTarget activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            return NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(activeBuildTarget));
        }

        [Serializable]
        private sealed class InspectorAsmdef
        {
            public string name;
            public string rootNamespace;
            public string[] references;
            public string[] includePlatforms;
            public string[] excludePlatforms;
            public bool allowUnsafeCode;
            public bool overrideReferences;
            public string[] precompiledReferences;
            public bool autoReferenced;
            public string[] defineConstraints;
            public VersionDefine[] versionDefines;
            public bool noEngineReferences;
        }

        [Serializable]
        private sealed class VersionDefine
        {
            public string name;
            public string expression;
            public string define;
        }
    }
}