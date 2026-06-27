using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Compilation;

namespace LoogaSoft.Inspector.Editor
{
    internal static class LoogaInspectorOptionalSupportUtility
    {
        public static bool AssemblyIsAvailable(string assemblyName)
        {
            if (CompilationPipeline.GetAssemblies().Any(assembly => assembly.name == assemblyName))
                return true;

            if (AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name == assemblyName))
                return true;

            return AssetDatabase.FindAssets($"{assemblyName} t:AssemblyDefinitionAsset").Length > 0;
        }

        public static bool AllAssembliesAreAvailable(IReadOnlyList<string> assemblyNames, out string missingAssemblies)
        {
            string[] missing = assemblyNames
                .Where(assemblyName => !AssemblyIsAvailable(assemblyName))
                .ToArray();

            missingAssemblies = string.Join(", ", missing);
            return missing.Length == 0;
        }

        public static bool DefineIsEnabled(string defineSymbol)
        {
            return GetDefines().Contains(defineSymbol);
        }

        public static void AddDefineSymbol(string defineSymbol)
        {
            List<string> defines = GetDefines();
            if (defines.Contains(defineSymbol))
                return;

            defines.Add(defineSymbol);
            ApplyDefineSymbols(defines);
        }

        public static void RemoveDefineSymbol(string defineSymbol)
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
    }
}
