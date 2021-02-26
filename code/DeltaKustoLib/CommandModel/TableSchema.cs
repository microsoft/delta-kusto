using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace DeltaKustoLib.CommandModel
{
    public class TableSchema : IEquatable<TableSchema>
    {
        public IImmutableList<TableSchema> Columns { get; }

        public TableSchema(IEnumerable<TableSchema> columns)
        {
            Columns = columns.ToImmutableArray();
        }

        public bool Equals(TableSchema? other)
        {
            return other != null;
        }

        public override string ToString()
        {
            var columnsText = string.Join(
                ", ",
                Columns.Select(c => c.ToString()));

            return $"{{ {columnsText} }}";
        }
    }
}