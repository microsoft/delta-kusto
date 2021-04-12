using System;
using System.Diagnostics.CodeAnalysis;

namespace DeltaKustoLib.CommandModel
{
    public class TableColumn : IEquatable<TableColumn>
    {
        public string ColumnName { get; }

        public string PrimitiveType { get; }

        public TableColumn(string columnName, string primitiveType)
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

        public bool Equals([AllowNull] TableColumn other)
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