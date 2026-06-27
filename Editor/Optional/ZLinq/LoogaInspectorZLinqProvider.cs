#if LOOGA_INSPECTOR_ZLINQ_SUPPORT
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using ZLinq;

namespace LoogaSoft.Inspector.Editor
{
    [InitializeOnLoad]
    internal static class LoogaInspectorZLinqProvider
    {
        static LoogaInspectorZLinqProvider()
        {
            LoogaInspectorQueryUtility.ObjectListProvider = ToObjectList;
            LoogaInspectorQueryUtility.StringListProvider = ToStringList;
            LoogaInspectorQueryUtility.AnimationClipNameProvider = GetAnimationClipNames;
            LoogaInspectorQueryUtility.AnimatorLayerNameProvider = GetAnimatorLayerNames;
            LoogaInspectorQueryUtility.SceneNameProvider = GetSceneNames;
            LoogaInspectorQueryUtility.SortingLayerNameProvider = GetSortingLayerNames;
            LoogaInspectorQueryUtility.AnimatorParameterFilterProvider = FilterAnimatorParameters;
            LoogaInspectorQueryUtility.ObjectLabelProvider = GetObjectLabels;
        }

        private static List<object> ToObjectList(IEnumerable source)
        {
            return source == null ? new List<object>() : source.AsValueEnumerable<object>().ToList();
        }

        private static List<string> ToStringList(IEnumerable<string> source)
        {
            return source == null ? new List<string>() : source.AsValueEnumerable().ToList();
        }

        private static List<string> GetAnimationClipNames(AnimationClip[] clips)
        {
            return clips == null ? new List<string>() : clips.AsValueEnumerable().Select(clip => clip != null ? clip.name : string.Empty).ToList();
        }

        private static List<string> GetAnimatorLayerNames(AnimatorControllerLayer[] layers)
        {
            return layers == null ? new List<string>() : layers.AsValueEnumerable().Select(layer => layer?.name ?? string.Empty).ToList();
        }

        private static List<string> GetSceneNames(EditorBuildSettingsScene[] scenes)
        {
            return scenes == null ? new List<string>() : scenes.AsValueEnumerable().Select(scene => System.IO.Path.GetFileNameWithoutExtension(scene.path)).ToList();
        }

        private static List<string> GetSortingLayerNames(SortingLayer[] sortingLayers)
        {
            return sortingLayers == null ? new List<string>() : sortingLayers.AsValueEnumerable().Select(layer => layer.name).ToList();
        }

        private static AnimatorControllerParameter[] FilterAnimatorParameters(
            AnimatorControllerParameter[] parameters,
            bool filterByParameterType,
            AnimatorControllerParameterType parameterType)
        {
            if (parameters == null)
                return Array.Empty<AnimatorControllerParameter>();

            return parameters
                .AsValueEnumerable()
                .Where(parameter => !filterByParameterType || parameter.type == parameterType)
                .ToArray();
        }

        private static string[] GetObjectLabels(List<object> options, Func<object, string> selector)
        {
            return options == null ? Array.Empty<string>() : options.AsValueEnumerable().Select(selector).ToArray();
        }
    }
}
#endif