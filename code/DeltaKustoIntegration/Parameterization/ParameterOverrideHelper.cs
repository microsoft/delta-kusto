using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DeltaKustoIntegration.Parameterization
{
    public static class ParameterOverrideHelper
    {
        #region Inner Types
        private class SingleOverride
        {
            public string? Path { get; set; }

            public object? Value { get; set; }
        }
        #endregion
        public static void InplaceOverride(object target, string jsonOverrides)
        {
            try
            {
                var overrideMap = JsonSerializer.Deserialize<SingleOverride[]>(
                    jsonOverrides,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var overrides = overrideMap
                    .Select(m => ValidateAndTransform(m));

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
            var isDictonary = target.GetType().IsGenericType
                && target.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>);

            if (isDictonary)
            {
                var arguments = target.GetType().GetGenericArguments();
                var keyType = arguments[0];
                var valueType = arguments[1];
                var genericMethod = typeof(ParameterOverrideHelper).GetMethod(
                    nameof(RecursiveInplaceOverrideOnDictionary),
                    BindingFlags.NonPublic | BindingFlags.Static);

                if (keyType != typeof(string))
                {
                    throw new NotSupportedException("We only support string-keyed map");
                }
                if (genericMethod == null)
                {
                    throw new NotSupportedException(
                        $"Can't find method '{nameof(RecursiveInplaceOverrideOnDictionary)}'");
                }

                var specificMethod = genericMethod.MakeGenericMethod(valueType);

                specificMethod.Invoke(null, new object[] { target, properties, value });
            }
            else
            {
                RecursiveInplaceOverrideOnObject(target, properties, value);
            }
        }

        private static void RecursiveInplaceOverrideOnDictionary<T>(
            IDictionary<string, T> target,
            IImmutableList<string> properties,
            object value) where T : class
        {
            var property = properties.First();

            if (properties.Count() > 1)
            {
                if (!target.ContainsKey(property))
                {
                    throw new DeltaException($"Property '{property}' doesn't exist on object");
                }

                var newTarget = target[property];

                if (newTarget == null)
                {
                    throw new DeltaException($"Property '{property}' is null");
                }

                RecursiveInplaceOverride(
                    newTarget,
                    properties.RemoveAt(0),
                    value);
            }
            else
            {
                if (value is JsonElement)
                {
                    try
                    {
                        var text = JsonSerializer.Serialize(value);
                        var newValue = JsonSerializer.Deserialize(
                            text,
                            typeof(T),
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        value = newValue;
                    }
                    catch (Exception ex)
                    {
                        throw new DeltaException(
                            $"Can't convert value to expected type ({typeof(T).FullName})",
                            ex);
                    }
                }

                var typedValue = value as T;

                if (typedValue == null)
                {
                    throw new DeltaException(
                        "The value isn't of the right type ; "
                        + $"value is {value.GetType().FullName} but "
                        + $"expecting {typeof(T).FullName}");
                }
                target[property] = typedValue;
            }
        }

        private static void RecursiveInplaceOverrideOnObject(
            object target,
            IImmutableList<string> properties,
            object value)
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
                    if (value is JsonElement)
                    {
                        try
                        {
                            var text = JsonSerializer.Serialize(value);
                            var newValue = JsonSerializer.Deserialize(
                                text,
                                propertyInfo.PropertyType,
                                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                            value = newValue;
                        }
                        catch (Exception ex)
                        {
                            throw new DeltaException(
                                $"Can't convert value to expected type ({propertyInfo.PropertyType.FullName})",
                                ex);
                        }
                    }
                    propertyInfo.GetSetMethod()!.Invoke(target, new object[] { value });
                }
                catch (DeltaException)
                {
                    throw;
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

        private static (string path, object value) ValidateAndTransform(SingleOverride m)
        {
            return (
                path: m.Path ?? throw new DeltaException("Path can't be null"),
                value: m.Value ?? throw new DeltaException("Value can't be null"));
        }
    }
}