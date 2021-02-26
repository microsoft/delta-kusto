using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class TypedParameter : IEquatable<TypedParameter>
    {
        public string ParameterName { get; }

        public string? PrimitiveType { get; }

        public TableSchema? ComplexType { get; }

        private TypedParameter(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentNullException(nameof(parameterName));
            }
            ParameterName = parameterName;
        }

        public TypedParameter(string parameterName, string primitiveType) : this(parameterName)
        {
            if (string.IsNullOrWhiteSpace(primitiveType))
            {
                throw new ArgumentNullException(nameof(primitiveType));
            }
            PrimitiveType = primitiveType;
        }

        public TypedParameter(string parameterName, TableSchema tableSchema)
            : this(parameterName)
        {
            ComplexType = tableSchema;
        }

        public bool Equals(TypedParameter? other)
        {
            return other != null
                && ParameterName == other.ParameterName
                && (PrimitiveType!=null
                ? PrimitiveType.Equals(other.PrimitiveType)
                : ComplexType!.Equals(other.ComplexType));
        }

        public override string ToString()
        {
            if (PrimitiveType != null)
            {
                return $"['{ParameterName}']:{PrimitiveType}";
            }
            else
            {
                return $"['{ParameterName}']:{ComplexType}";
            }
        }
    }
}