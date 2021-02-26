using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class ColumnSchema : IEquatable<ColumnSchema>
    {
        public string ColumnName { get; }

        public string PrimitiveType { get; }

        public ColumnSchema(string columnName, string primitiveType)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new ArgumentNullException(nameof(columnName));
            }
            if (string.IsNullOrWhiteSpace(primitiveType))
            {
                throw new ArgumentNullException(nameof(primitiveType));
            }
            ColumnName = columnName;
            PrimitiveType = primitiveType;
        }

        public bool Equals([AllowNull] ColumnSchema other)
        {
            return other != null
                && ColumnName == other.ColumnName
                && PrimitiveType == other.PrimitiveType;
        }

        public override string ToString()
        {
            return $"['{ColumnName}']:{PrimitiveType}";
        }
    }
}