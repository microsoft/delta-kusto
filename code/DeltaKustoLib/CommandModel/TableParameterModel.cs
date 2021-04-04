using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DeltaKustoLib.CommandModel
{
    public class TableParameterModel : IEquatable<TableParameterModel>
    {
        public IImmutableList<ColumnModel> Columns { get; }

        public TableParameterModel(IEnumerable<ColumnModel> columns)
        {
            Columns = columns.ToImmutableArray();
        }

        public bool Equals(TableParameterModel? other)
        {
            return other != null
                && Columns.Count == other.Columns.Count
                && Columns
                .Zip(other.Columns, (c1, c2) => c1.Equals(c2))
                .All(p => p);
        }

        public override string ToString()
        {
            if (Columns.Any())
            {
                var columnsText = string.Join(
                    ", ",
                    Columns.Select(c => c.ToString()));

                return $"({columnsText})";
            }
            else
            {
                return "(*)";
            }
        }
    }
}