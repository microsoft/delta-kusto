using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class TypedParameterModel : IEquatable<TypedParameterModel>
    {
        public string ParameterName { get; }

        public string? PrimitiveType { get; }

        public TableParameterModel? ComplexType { get; }

        private TypedParameterModel(string parameterName)
        {
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                throw new ArgumentNullException(nameof(parameterName));
            }
            ParameterName = parameterName;
        }

        public TypedParameterModel(string parameterName, string primitiveType) : this(parameterName)
        {
            if (string.IsNullOrWhiteSpace(primitiveType))
            {
                throw new ArgumentNullException(nameof(primitiveType));
            }
            PrimitiveType = primitiveType;
        }

        public TypedParameterModel(string parameterName, TableParameterModel tableSchema)
            : this(parameterName)
        {
            ComplexType = tableSchema;
        }

        public bool Equals(TypedParameterModel? other)
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