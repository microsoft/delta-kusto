using DeltaKustoLib.CommandModel;
using System;

namespace DeltaKustoLib.KustoModel
{
    public class ColumnModel
    {
        public EntityName ColumnName { get; }

        public string PrimitiveType { get; }

        public QuotedText? DocString { get; }

        public ColumnModel(
            EntityName columnName,
            string primitiveType,
            QuotedText? docString)
        {
            if (string.IsNullOrWhiteSpace(primitiveType))
            {
                throw new ArgumentNullException(nameof(primitiveType));
            }
            ColumnName = columnName;
            PrimitiveType = primitiveType;
            DocString = docString;
        }

        public override bool Equals(object? other)
        {
            var otherColumn = other as ColumnModel;

            return otherColumn != null
                && ColumnName.Equals(otherColumn.ColumnName)
                && PrimitiveType.Equals(otherColumn.PrimitiveType)
                && object.Equals(DocString, otherColumn.DocString);
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