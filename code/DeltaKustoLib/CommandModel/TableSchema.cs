using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DeltaKustoLib.CommandModel
{
    public class TableSchema : IEquatable<TableSchema>
    {
        public IImmutableList<ColumnSchema> Columns { get; }

        public TableSchema(IEnumerable<ColumnSchema> columns)
        {
            Columns = columns.ToImmutableArray();
        }

        public bool Equals(TableSchema? other)
        {
            return other != null
                && Columns.Count == other.Columns.Count
                && Columns
                .Zip(other.Columns, (c1, c2) => c1.Equals(c2))
                .All(p => p);
        }

        public override string ToString()
        {
            var columnsText = string.Join(
                ", ",
                Columns.Select(c => c.ToString()));

            return $"({columnsText})";
        }
    }
}