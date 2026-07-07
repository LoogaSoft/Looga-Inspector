using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LoogaSoft.Inspector.Editor
{
    public static class LoogaInspectorQueryUtility
    {
        public static Func<IEnumerable, List<object>> ObjectListProvider { private get; set; } = DefaultObjectList;
        public static Func<IEnumerable<string>, List<string>> StringListProvider { private get; set; } = DefaultStringList;
        public static Func<AnimationClip[], List<string>> AnimationClipNameProvider { private get; set; } = DefaultAnimationClipNames;
        public static Func<AnimatorControllerLayer[], List<string>> AnimatorLayerNameProvider { private get; set; } = DefaultAnimatorLayerNames;
        public static Func<EditorBuildSettingsScene[], List<string>> SceneNameProvider { private get; set; } = DefaultSceneNames;
        public static Func<SortingLayer[], List<string>> SortingLayerNameProvider { private get; set; } = DefaultSortingLayerNames;
        public static Func<AnimatorControllerParameter[], bool, AnimatorControllerParameterType, AnimatorControllerParameter[]> AnimatorParameterFilterProvider { private get; set; } = DefaultAnimatorParameterFilter;
        public static Func<List<object>, Func<object, string>, string[]> ObjectLabelProvider { private get; set; } = DefaultObjectLabels;

        public static List<object> ToObjectList(IEnumerable source) => ObjectListProvider(source);
        public static List<string> ToStringList(IEnumerable<string> source) => StringListProvider(source);
        public static List<string> GetAnimationClipNames(AnimationClip[] clips) => AnimationClipNameProvider(clips);
        public static List<string> GetAnimatorLayerNames(AnimatorControllerLayer[] layers) => AnimatorLayerNameProvider(layers);
        public static List<string> GetSceneNames(EditorBuildSettingsScene[] scenes) => SceneNameProvider(scenes);
        public static List<string> GetSortingLayerNames(SortingLayer[] sortingLayers) => SortingLayerNameProvider(sortingLayers);
        public static AnimatorControllerParameter[] FilterAnimatorParameters(AnimatorControllerParameter[] parameters, bool filterByParameterType, AnimatorControllerParameterType parameterType) => AnimatorParameterFilterProvider(parameters, filterByParameterType, parameterType);
        public static string[] GetObjectLabels(List<object> options, Func<object, string> selector) => ObjectLabelProvider(options, selector);

        private static List<object> DefaultObjectList(IEnumerable source)
        {
            List<object> values = new();
            if (source == null)
                return values;

            foreach (object item in source)
                values.Add(item);

            return values;
        }

        private static List<string> DefaultStringList(IEnumerable<string> source)
        {
            List<string> values = new();
            if (source == null)
                return values;

            foreach (string item in source)
                values.Add(item);

            return values;
        }

        private static List<string> DefaultAnimationClipNames(AnimationClip[] clips)
        {
            List<string> names = new(clips?.Length ?? 0);
            if (clips == null)
                return names;

            for (int i = 0; i < clips.Length; i++)
                names.Add(clips[i] != null ? clips[i].name : string.Empty);

            return names;
        }

        private static List<string> DefaultAnimatorLayerNames(AnimatorControllerLayer[] layers)
        {
            List<string> names = new(layers?.Length ?? 0);
            if (layers == null)
                return names;

            for (int i = 0; i < layers.Length; i++)
                names.Add(layers[i]?.name ?? string.Empty);

            return names;
        }

        private static List<string> DefaultSceneNames(EditorBuildSettingsScene[] scenes)
        {
            List<string> names = new(scenes?.Length ?? 0);
            if (scenes == null)
                return names;

            for (int i = 0; i < scenes.Length; i++)
                names.Add(System.IO.Path.GetFileNameWithoutExtension(scenes[i].path));

            return names;
        }

        private static List<string> DefaultSortingLayerNames(SortingLayer[] sortingLayers)
        {
            List<string> names = new(sortingLayers?.Length ?? 0);
            if (sortingLayers == null)
                return names;

            for (int i = 0; i < sortingLayers.Length; i++)
                names.Add(sortingLayers[i].name);

            return names;
        }

        private static AnimatorControllerParameter[] DefaultAnimatorParameterFilter(
            AnimatorControllerParameter[] parameters,
            bool filterByParameterType,
            AnimatorControllerParameterType parameterType)
        {
            if (parameters == null || parameters.Length == 0)
                return Array.Empty<AnimatorControllerParameter>();

            if (!filterByParameterType)
                return parameters;

            List<AnimatorControllerParameter> filtered = new(parameters.Length);
            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];
                if (parameter.type == parameterType)
                    filtered.Add(parameter);
            }

            return filtered.ToArray();
        }

        private static string[] DefaultObjectLabels(List<object> options, Func<object, string> selector)
        {
            if (options == null || options.Count == 0)
                return Array.Empty<string>();

            string[] labels = new string[options.Count];
            for (int i = 0; i < options.Count; i++)
                labels[i] = selector(options[i]);

            return labels;
        }
    }
}