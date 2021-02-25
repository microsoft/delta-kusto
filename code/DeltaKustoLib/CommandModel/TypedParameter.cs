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
            ParameterName = parameterName;
            Type = type;
        }

        public bool Equals([AllowNull] TypedParameter other)
        {
            return other != null
                && ParameterName == other.ParameterName
                && Type == other.Type;
        }
    }
}