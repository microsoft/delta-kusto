using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace DeltaKustoIntegration.Parameterization
{
    public static class ParameterOverrideHelper
    {
        public static void InplaceOverride(object target, string jsonOverrides)
        {
            try
            {
                var overrideMap = JsonSerializer.Deserialize<IDictionary<string, object>>(jsonOverrides);
                var overrides = overrideMap
                    .Select(m => (path: m.Key, value: m.Value));

                InplaceOverride(target, overrides);
            }
            catch (DeltaException ex)
            {
                throw new DeltaException(
                    $"Issue with the following JSON parameter override:  '{jsonOverrides}'",
                    ex);
            }
            catch (JsonException ex)
            {
                throw new DeltaException(
                    $"The following string doesn't represent a valid JSON object:  '{jsonOverrides}'",
                    ex);
            }
        }

        public static void InplaceOverride(object target, IEnumerable<(string path, object value)> overrides)
        {
            foreach (var o in overrides)
            {
                InplaceOverride(target, o.path, o.value);
            }
        }

        public static void InplaceOverride(object target, string path, object value)
        {
            var properties = path
                .Split('.')
                .ToImmutableArray();

            try
            {
                if (value == null)
                {
                    throw new DeltaException("Value is null");
                }

                ValidateProperties(properties);

                RecursiveInplaceOverride(target, properties, value);
            }
            catch (DeltaException ex)
            {
                throw new DeltaException($"Issue with override property path '{path}'", ex);
            }
        }

        private static void RecursiveInplaceOverride(object target, IImmutableList<string> properties, object value)
        {
            var jsonProperty = properties.First();
            var property = GetRealProperty(jsonProperty);
            var propertyInfo = target.GetType().GetProperty(property);

            if (propertyInfo == null)
            {
                throw new DeltaException($"Property '{jsonProperty}' doesn't exist on object");
            }

            if (properties.Count() > 1)
            {
                var newTarget = propertyInfo.GetGetMethod()!.Invoke(target, new object[0]);

                if (newTarget == null)
                {
                    throw new DeltaException($"Property '{jsonProperty}' is null");
                }

                RecursiveInplaceOverride(
                    newTarget,
                    properties.RemoveAt(0),
                    value);
            }
            else
            {
                try
                {
                    propertyInfo.GetSetMethod()!.Invoke(target, new object[] { value });
                }
                catch (Exception ex)
                {
                    throw new DeltaException(
                        "The value isn't of the right type ; "
                        + $"value is {value.GetType().FullName} but "
                        + $"expecting {propertyInfo.PropertyType.FullName}",
                        ex);
                }
            }
        }

        private static string GetRealProperty(string jsonPropertyName)
        {
            var property = char.ToUpper(jsonPropertyName[0]) + jsonPropertyName.Substring(1);

            return property;
        }

        private static void ValidateProperties(IEnumerable<string> properties)
        {
            foreach (var property in properties)
            {
                ValidateProperty(property);
            }
        }

        private static void ValidateProperty(string property)
        {
            if (property.Length == 0)
            {
                throw new DeltaException("Empty property within property path");
            }

            var illegalCharacter = property
                .Where(c => !(char.IsLetter(c) || char.IsDigit(c) || c != '_'))
                .FirstOrDefault();

            if (illegalCharacter != default(char))
            {
                throw new DeltaException(
                    $"Illegal character '{illegalCharacter}' in property path '{property}'");
            }
        }
    }
}