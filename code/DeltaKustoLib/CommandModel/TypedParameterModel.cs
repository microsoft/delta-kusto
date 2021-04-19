using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class TypedParameterModel : IEquatable<TypedParameterModel>
    {
        public EntityName ParameterName { get; }

        public string? PrimitiveType { get; }

        public string? DefaultValue { get; }

        public TableParameterModel? ComplexType { get; }

        private TypedParameterModel(EntityName parameterName)
        {
            ParameterName = parameterName;
        }

        public TypedParameterModel(
            EntityName parameterName,
            string primitiveType,
            string? defaultValue) : this(parameterName)
        {
            if (string.IsNullOrWhiteSpace(primitiveType))
            {
                throw new ArgumentNullException(nameof(primitiveType));
            }
            PrimitiveType = primitiveType;
            DefaultValue = defaultValue;
        }

        public TypedParameterModel(EntityName parameterName, TableParameterModel tableSchema)
            : this(parameterName)
        {
            ComplexType = tableSchema;
        }

        public bool Equals(TypedParameterModel? other)
        {
            return other != null
                && ParameterName.Equals(other.ParameterName)
                && (PrimitiveType != null
                ? PrimitiveType.Equals(other.PrimitiveType) && DefaultValue == other.DefaultValue
                : ComplexType!.Equals(other.ComplexType));
        }

        public override string ToString()
        {
            if (PrimitiveType != null)
            {
                return $"{ParameterName}:{PrimitiveType}{DefaultValue}";
            }
            else
            {
                return $"{ParameterName}:{ComplexType}";
            }
        }
    }
}