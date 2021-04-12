using System;

namespace DeltaKustoLib.KustoModel
{
    public class ColumnModel
    {
        public string ColumnName { get; }

        public string PrimitiveType { get; }

        public string? DocString { get; }

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

        public override bool Equals(object? other)
        {
            var otherColumn = other as ColumnModel;

            return otherColumn != null
                && ColumnName == otherColumn.ColumnName
                && PrimitiveType == otherColumn.PrimitiveType
                && string.Equals(DocString, otherColumn.DocString);
        }

        public override int GetHashCode()
        {
            return ColumnName.GetHashCode()
                ^ PrimitiveType.GetHashCode()
                ^ (DocString != null ? DocString.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"['{ColumnName}']:{PrimitiveType}"
                + (DocString == null ? string.Empty : $"('{DocString}')");
        }
    }
}