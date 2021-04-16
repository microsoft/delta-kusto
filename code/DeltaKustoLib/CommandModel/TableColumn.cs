using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public class TableColumn : IEquatable<TableColumn>
    {
        public EntityName ColumnName { get; }

        public string PrimitiveType { get; }

        public TableColumn(EntityName columnName, string primitiveType)
        {
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
                && ColumnName.Equals(other.ColumnName)
                && PrimitiveType == other.PrimitiveType;
        }

        public override string ToString()
        {
            return $"{ColumnName}:{PrimitiveType}";
        }
    }
}