using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;

namespace DeltaKustoIntegration.Parameterization
{
    public static class ParameterOverrideHelper
    {
        public static void InplaceOverride(object target, params string[] pathOverrides)
        {
            InplaceOverride(target, (IEnumerable<string>)pathOverrides);
        }

        public static void InplaceOverride(object target, IEnumerable<string> pathOverrides)
        {
            if (pathOverrides.Any())
            {
                try
                {
                    var splits = pathOverrides.Select(t => t.Split('='));
                    var noEquals = splits.FirstOrDefault(s => s.Length != 2);

                    if (noEquals != null)
                    {
                        throw new DeltaException(
                            $"Override must be of the form path=value ; "
                            + $"exception:  '{string.Join('=', noEquals)}'");
                    }

                    var overrides = splits.Select(s => (path: s[0], textValue: s[1]));

                    InplaceOverride(target, overrides);
                }
                catch (Exception ex)
                {
                    throw new DeltaException(
                        $"Issue with the following JSON parameter override:  '{pathOverrides}'",
                        ex);
                }
            }
        }

        public static void InplaceOverride(
            object target,
            IEnumerable<(string path, string textValue)> overrides)
        {
            if (overrides is null)
            {
                throw new ArgumentNullException(nameof(overrides));
            }

            foreach (var o in overrides)
            {
                InplaceOverride(target, o.path, o.textValue);
            }
        }

        public static void InplaceOverride(object target, string path, string textValue)
        {
            //  Create a stack to efficiently recurse over the properties
            var properties = ImmutableStack<string>.Empty;

            foreach (var p in path.Split('.').Reverse())
            {
                properties = properties.Push(p);
            }

            try
            {
                ValidateProperties(properties);

                RecursiveInplaceOverride(target, properties, textValue);
            }
            catch (DeltaException ex)
            {
                throw new DeltaException($"Issue with override property path '{path}'", ex);
            }
        }

        private static void RecursiveInplaceOverride(
            object target,
            IImmutableStack<string> properties,
            string textValue)
        {   //  Determine if target is a dictionary or object
            var isDictionary = target.GetType().IsGenericType
                && target.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>);

            if (isDictionary)
            {   //  Recreate generic method to call strong-type
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

                specificMethod.Invoke(null, new object[] { target, properties, textValue });
            }
            else
            {
                RecursiveInplaceOverrideOnObject(target, properties, textValue);
            }
        }

        private static void RecursiveInplaceOverrideOnDictionary<T>(
            IDictionary<string, T> target,
            IImmutableStack<string> properties,
            string textValue) where T : class, new()
        {
            var property = properties.Peek();
            var remainingProperties = properties.Pop();

            if (remainingProperties.IsEmpty)
            {
                throw new DeltaException($"Can't override a dictionary at '{property}'");
            }
            //  If the key doesn't exist in the dictionary, we create it
            if (!target.ContainsKey(property))
            {
                target[property] = new T();
            }

            var newTarget = target[property];

            if (newTarget == null)
            {
                throw new DeltaException($"Property '{property}' is null");
            }

            RecursiveInplaceOverride(
                newTarget,
                remainingProperties,
                textValue);
        }

        private static void RecursiveInplaceOverrideOnObject(
            object target,
            IImmutableStack<string> properties,
            string textValue)
        {
            var property = properties.Peek();
            var realProperty = GetRealProperty(property);
            var propertyInfo = target.GetType().GetProperty(realProperty);
            var remainingProperties = properties.Pop();

            if (propertyInfo == null)
            {
                throw new DeltaException($"Property '{property}' doesn't exist on object");
            }

            if (!remainingProperties.IsEmpty)
            {
                var newTarget = propertyInfo.GetGetMethod()!.Invoke(target, new object[0]);

                if (newTarget == null)
                {   //  Property is null, we try to create it
                    newTarget = propertyInfo
                        .PropertyType
                        .GetConstructor(new Type[0])
                        ?.Invoke(null);

                    if (newTarget == null)
                    {
                        throw new InvalidOperationException(
                            $"Failed constructor on type '{newTarget}'");
                    }

                    propertyInfo.GetSetMethod()!.Invoke(target, new[] { newTarget });
                }

                RecursiveInplaceOverride(
                    newTarget,
                    remainingProperties,
                    textValue);
            }
            else
            {
                var value = ParseValue(textValue, propertyInfo.PropertyType);

                propertyInfo.GetSetMethod()!.Invoke(target, new object[] { value });
            }
        }

        private static object ParseValue(string textValue, Type type)
        {
            if (type == typeof(string))
            {
                return textValue;
            }
            else if (type == typeof(bool))
            {
                var upperTextValue = textValue.ToLower();

                if (upperTextValue == "true")
                {
                    return true;
                }
                else if (upperTextValue == "false")
                {
                    return false;
                }
                else
                {
                    throw new DeltaException($"Value '{textValue}' should be a boolean");
                }
            }
            else if (type == typeof(int))
            {
                int value;

                if (!int.TryParse(textValue, out value))
                {
                    throw new DeltaException($"Value '{textValue}' should be an integer");
                }
                else
                {
                    return value;
                }
            }
            else
            {
                throw new DeltaException($"Type '{type.Name}' isn't supported in override");
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