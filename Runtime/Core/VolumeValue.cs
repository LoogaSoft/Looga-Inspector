using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace LoogaSoft.Inspector.Runtime
{
    [Serializable]
    public struct VolumeValue<T>
    {
        private const BindingFlags ParameterFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        [SerializeField, HideInInspector] private VolumeProfile _volumeProfile;
        [SerializeField, HideInInspector] private string _componentTypeName;
        [SerializeField, HideInInspector] private string _parameterName;

        public readonly VolumeProfile VolumeProfile => _volumeProfile;
        public readonly string ComponentTypeName => _componentTypeName;
        public readonly string ParameterName => _parameterName;
        public readonly bool IsAssigned => _volumeProfile != null
            && !string.IsNullOrWhiteSpace(_componentTypeName)
            && !string.IsNullOrWhiteSpace(_parameterName);

        public void SetReference(VolumeProfile volumeProfile, string componentTypeName, string parameterName)
        {
            _volumeProfile = volumeProfile;
            _componentTypeName = componentTypeName;
            _parameterName = parameterName;
        }

        public readonly T GetValue()
        {
            return GetValue(_volumeProfile);
        }

        public readonly T GetValue(VolumeProfile overrideVolumeProfile)
        {
            return TryGetParameter(overrideVolumeProfile, out VolumeParameter<T> parameter)
                ? parameter.value
                : default;
        }

        public readonly bool TryGetValue(out T value)
        {
            return TryGetValue(_volumeProfile, out value);
        }

        public readonly bool TryGetValue(VolumeProfile overrideVolumeProfile, out T value)
        {
            if (TryGetParameter(overrideVolumeProfile, out VolumeParameter<T> parameter))
            {
                value = parameter.value;
                return true;
            }

            value = default;
            return false;
        }

        public void SetValue(T value)
        {
            SetValue(value, _volumeProfile);
        }

        public readonly void SetValue(T value, VolumeProfile overrideVolumeProfile)
        {
            if (TryGetParameter(overrideVolumeProfile, out VolumeParameter<T> parameter))
                parameter.value = value;
        }

        public readonly bool TryGetParameter(out VolumeParameter<T> parameter)
        {
            return TryGetParameter(_volumeProfile, out parameter);
        }

        public readonly bool TryGetParameter(VolumeProfile overrideVolumeProfile, out VolumeParameter<T> parameter)
        {
            parameter = null;
            VolumeComponent component = ResolveComponent(overrideVolumeProfile);
            if (component == null || string.IsNullOrWhiteSpace(_parameterName))
                return false;

            FieldInfo field = component.GetType().GetField(_parameterName, ParameterFlags);
            if (field == null || field.GetValue(component) is not VolumeParameter<T> volumeParameter)
                return false;

            parameter = volumeParameter;
            return true;
        }

        private readonly VolumeComponent ResolveComponent(VolumeProfile profile)
        {
            if (profile == null || string.IsNullOrWhiteSpace(_componentTypeName))
                return null;

            for (int i = 0; i < profile.components.Count; i++)
            {
                VolumeComponent component = profile.components[i];
                Type componentType = component != null ? component.GetType() : null;
                if (componentType == null)
                    continue;

                if (componentType.AssemblyQualifiedName == _componentTypeName || componentType.FullName == _componentTypeName)
                    return component;
            }

            return null;
        }
    }
}
