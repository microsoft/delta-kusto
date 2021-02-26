using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class TypedParameter : IEquatable<TypedParameter>
    {
        public string ParameterName { get; }

        public string Type { get; }

        public TypedParameter(string parameterName, string type)
        {
            if(string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentNullException(nameof(parameterName));
            }
            if (string.IsNullOrWhiteSpace(type))
            {
                throw new ArgumentNullException(nameof(type));
            }
            ParameterName = parameterName;
            Type = type;
        }

        public bool Equals([AllowNull] TypedParameter other)
        {
            return other != null
                && ParameterName == other.ParameterName
                && Type == other.Type;
        }

        public override string ToString()
        {
            return $"['{ParameterName}']:{Type}";
        }
    }
}