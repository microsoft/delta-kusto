using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class ColumnModel : IEquatable<ColumnModel>
    {
        public string ColumnName { get; }

        public string PrimitiveType { get; }

        public ColumnModel(string columnName, string primitiveType)
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

        public bool Equals([AllowNull] ColumnModel other)
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